using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Services;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Tests.Scraping.Services
{
    public class GroupScheduleCollectorServiceTests
    {
        private async IAsyncEnumerable<WeekAssessment> StreamOf(IEnumerable<WeekAssessment> assessments)
        {
            foreach (var assessment in assessments)
            {
                await Task.Yield();
                yield return assessment;
            }
        }

        private WeekAssessment Accepted(string groupName, int week, int sessionCount)
        {
            var sessions = Enumerable.Range(0, sessionCount).Select(_ => new ScheduledSession { WeekNumber = week }).ToList();

            return new WeekAssessment(groupName, week, WeekResponseStatus.Complete, sessions, "ok");
        }

        private WeekAssessment VerifiedEmpty(string groupName, int week)
        {
            return new WeekAssessment(groupName, week, WeekResponseStatus.CompleteEmpty, new List<ScheduledSession>(), "vide");
        }

        private WeekAssessment Rejected(string groupName, int week)
        {
            return new WeekAssessment(groupName, week, WeekResponseStatus.Invalid, new List<ScheduledSession>(), "invalide");
        }

        private async Task<List<GroupSchedule>> CollectAsync(params WeekAssessment[] assessments)
        {
            var schedules = new List<GroupSchedule>();

            await foreach (var schedule in new GroupScheduleCollectorService().CollectAsync(StreamOf(assessments)))
            {
                schedules.Add(schedule);
            }

            return schedules;
        }

        [Fact]
        public async Task CollectAsync_TwoGroups_ReturnsOneScheduleEach()
        {
            var schedules = await CollectAsync(Accepted("IODA B3", 8, 9), Accepted("IODA B3", 11, 15), Accepted("SI 3", 11, 15));

            Assert.Equal(2, schedules.Count);
            Assert.Equal(new[] { 8, 11 }, schedules[0].WeekNumbers);
            Assert.Equal(24, schedules[0].Sessions.Count);
        }

        [Fact]
        public async Task CollectAsync_RejectedWeek_IsNotPartOfTheCoveredWeeks()
        {
            var schedules = await CollectAsync(Rejected("IODA B3", 8), Accepted("IODA B3", 11, 15));

            var schedule = Assert.Single(schedules);
            Assert.Equal(new[] { 11 }, schedule.WeekNumbers);
        }

        [Fact]
        public async Task CollectAsync_VerifiedEmptyWeek_IsCoveredWithoutSessions()
        {
            var schedules = await CollectAsync(VerifiedEmpty("IODA B3", 7), Accepted("IODA B3", 8, 9));

            var schedule = Assert.Single(schedules);
            Assert.Equal(new[] { 7, 8 }, schedule.WeekNumbers);
            Assert.Equal(9, schedule.Sessions.Count);
        }

        [Fact]
        public async Task CollectAsync_OnlyRejectedWeeks_ReturnsNothing()
        {
            var schedules = await CollectAsync(Rejected("IODA B3", 8), Rejected("IODA B3", 11));

            Assert.Empty(schedules);
        }
    }
}
