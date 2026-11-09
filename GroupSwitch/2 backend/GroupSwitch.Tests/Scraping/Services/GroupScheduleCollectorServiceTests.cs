using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Services;
using GroupSwitch.Tests.Scraping.TestSupport;

namespace GroupSwitch.Tests.Scraping.Services
{
    public class GroupScheduleCollectorServiceTests
    {
        private readonly GroupScheduleCollectorService groupScheduleCollectorService = new(TestObjects.NewWeekResponseConverterService());

        private IAsyncEnumerable<WeekRetrieval> StreamOf(List<WeekRetrieval> responses, bool failAtTheEnd = false)
        {
            var groupNames = responses.Select(response => response.GroupName).Distinct().ToList();
            var weeks = responses.Select(response => response.RequestedWeek).Distinct().ToList();

            return new FakeHyperplanningScraper(responses, failAtTheEnd).ScrapeAsync(groupNames, weeks, CancellationToken.None);
        }

        [Fact]
        public async Task Collect_TwoGroups_YieldsOneScheduleForEachGroup()
        {
            var groups = new List<GroupSchedule>();

            await foreach (var group in groupScheduleCollectorService.CollectAsync(StreamOf(SampleData.TwoGroupsResponses()), week => true))
            {
                groups.Add(group);
            }

            Assert.Equal(2, groups.Count);
            Assert.Equal("IODA B3", groups.First().GroupName);
            Assert.Equal(24, groups.First().Sessions.Count);
            Assert.Equal("SI 3", groups.Last().GroupName);
            Assert.Equal(15, groups.Last().Sessions.Count);
            Assert.Equal(new[] { 8, 11 }, groups.First().WeekNumbers);
            Assert.Equal(new[] { 11 }, groups.Last().WeekNumbers);
        }

        [Fact]
        public async Task Collect_EmptyStream_YieldsNothing()
        {
            var groups = new List<GroupSchedule>();

            await foreach (var group in groupScheduleCollectorService.CollectAsync(StreamOf(new List<WeekRetrieval>()), week => true))
            {
                groups.Add(group);
            }

            Assert.Empty(groups);
        }

        [Fact]
        public async Task Collect_ScraperFails_StillYieldsTheGroupsAlreadyComplete()
        {
            var groups = new List<GroupSchedule>();
            var responses = StreamOf(SampleData.TwoGroupsResponses(), true);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var group in groupScheduleCollectorService.CollectAsync(responses, week => true))
                {
                    groups.Add(group);
                }
            });

            Assert.Single(groups);
            Assert.Equal("IODA B3", groups.First().GroupName);
        }

        [Fact]
        public async Task Collect_WeekWithoutSessions_StillReportsTheScrapedWeek()
        {
            var emptyWeekJson = """{"donneesSec":{"donnees":{"ListeCours":[],"ListeAnnulationsCours":[]}}}""";
            var responses = new List<WeekRetrieval> { WeekRetrieval.Succeeded("IODA B3", 9, emptyWeekJson) };
            var groups = new List<GroupSchedule>();

            await foreach (var group in groupScheduleCollectorService.CollectAsync(StreamOf(responses), week => true))
            {
                groups.Add(group);
            }

            var schedule = Assert.Single(groups);
            Assert.Equal(new[] { 9 }, schedule.WeekNumbers);
            Assert.Empty(schedule.Sessions);
        }
    }
}