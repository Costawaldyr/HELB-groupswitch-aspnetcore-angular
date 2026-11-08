using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;
using GroupSwitch.Business.Services;
using GroupSwitch.Tests.Scraping;
using GroupSwitch.Tests.Utils;

namespace GroupSwitch.Tests.Services
{
    public class GroupScheduleCollectorTests
    {
        private readonly GroupScheduleCollector collector = new(TestObjects.NewConverter());

        private IAsyncEnumerable<WeekResponse> StreamOf(List<WeekResponse> responses, bool failAtTheEnd = false)
        {
            return new FakeHyperplanningScraper(responses, failAtTheEnd).ScrapeAsync(name => true, week => true);
        }

        [Fact]
        public async Task Collect_TwoGroups_YieldsOneScheduleForEachGroup()
        {
            var groups = new List<GroupSchedule>();

            await foreach (var group in collector.CollectAsync(StreamOf(SampleData.TwoGroupsResponses()), QuadrimesterFilters.IsInFirstQuadrimester))
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

            await foreach (var group in collector.CollectAsync(StreamOf(new List<WeekResponse>()), QuadrimesterFilters.IsInFirstQuadrimester))
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
                await foreach (var group in collector.CollectAsync(responses, QuadrimesterFilters.IsInFirstQuadrimester))
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
            var responses = new List<WeekResponse> { new("IODA B3", 9, emptyWeekJson) };
            var groups = new List<GroupSchedule>();

            await foreach (var group in collector.CollectAsync(StreamOf(responses), QuadrimesterFilters.IsInFirstQuadrimester))
            {
                groups.Add(group);
            }

            var schedule = Assert.Single(groups);
            Assert.Equal(new[] { 9 }, schedule.WeekNumbers);
            Assert.Empty(schedule.Sessions);
        }
    }
}