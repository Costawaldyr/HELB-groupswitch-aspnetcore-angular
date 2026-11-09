using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Parsing;
using GroupSwitch.Business.Scraping.Retrieval;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace GroupSwitch.Tests.Scraping.Retrieval
{
    [Trait("Category", "Online")]
    public class PlaywrightScraperOnlineTests
    {
        private const string GroupName = "IODA B3";
        private const int ResponseHeadLength = 300;

        private readonly ITestOutputHelper _output;

        public PlaywrightScraperOnlineTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private PlaywrightHyperplanningScraper NewScraper()
        {
            var settings = Options.Create(new HyperplanningSettings
            {
                PageUrl = "https://planning.helb-prigogine.be/2026-2027/invite",
                Headless = true
            });

            return new PlaywrightHyperplanningScraper(new GroupListParser(), settings);
        }

        [Fact]
        public async Task GetGroupNamesAsync_RealSite_ContainsTheGroup()
        {
            var groupNames = await NewScraper().GetGroupNamesAsync(CancellationToken.None);

            Assert.Contains(GroupName, groupNames);
        }

        [Fact]
        public async Task ScrapeAsync_OneGroupThreeWeeks_ReturnsOneRetrievalPerWeek()
        {
            var retrievals = new List<WeekRetrieval>();

            await foreach (var retrieval in NewScraper().ScrapeAsync(new[] { GroupName }, new[] { 7, 8, 11 }, CancellationToken.None))
            {
                retrievals.Add(retrieval);
                _output.WriteLine($"Week {retrieval.RequestedWeek} | ok={retrieval.IsSuccessful} | reason={retrieval.FailureReason}");
                _output.WriteLine($"   response: {Describe(retrieval.Json)}");
            }

            var classifier = new ResponseClassifier();
            var requestParser = new ScheduleRequestParser();
            var weekEleven = retrievals.Single(retrieval => retrieval.RequestedWeek == 11);

            Assert.Equal(new[] { 7, 8, 11 }, retrievals.Select(retrieval => retrieval.RequestedWeek));
            Assert.All(retrievals, retrieval => Assert.Equal(GroupName, retrieval.GroupName));
            Assert.All(retrievals, retrieval => Assert.True(retrieval.IsSuccessful, retrieval.FailureReason));
            Assert.All(retrievals, retrieval =>
            {
                var request = requestParser.Parse(retrieval.RequestBody);

                Assert.NotNull(request);
                Assert.Equal(GroupName, request.GroupName);
                Assert.Equal(retrieval.RequestedWeek, request.Week);
            });
            Assert.NotNull(weekEleven.Json);
            Assert.True(classifier.IsSchedule(weekEleven.Json));
            Assert.NotEmpty(new WeekScheduleParser().Parse(weekEleven.Json));

            var assessmentService = TestObjects.NewWeekAssessmentService();
            var assessments = retrievals.Select(assessmentService.Assess).ToList();

            foreach (var assessment in assessments)
            {
                _output.WriteLine($"Assessment week {assessment.Week} | {assessment.Status} | {assessment.Diagnostic}");
            }

            Assert.Equal(WeekResponseStatus.CompleteEmpty, assessments.Single(assessment => assessment.Week == 7).Status);
            Assert.Equal(WeekResponseStatus.Complete, assessments.Single(assessment => assessment.Week == 8).Status);
            Assert.Equal(WeekResponseStatus.Complete, assessments.Single(assessment => assessment.Week == 11).Status);
        }

        private string Describe(string? json)
        {
            if (json is null)
            {
                return "no json";
            }

            var head = json.Length <= ResponseHeadLength ? json : json[..ResponseHeadLength];

            return $"length={json.Length} hasListeCours={json.Contains("\"ListeCours\"")} head={head}";
        }
    }
}
