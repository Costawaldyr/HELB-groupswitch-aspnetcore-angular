using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Services;
using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.Extensions.Options;

namespace GroupSwitch.Tests.Scraping.Services
{
    public class ScheduleQueryServiceTests
    {
        private const string GroupName = "IODA B3";
        private const string AcademicYear = "2026-2027";

        private readonly DateTime retrievedAtUtc = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        private ScheduleQueryService NewService(SqliteTestDatabase database)
        {
            return new ScheduleQueryService(
                new ScheduleRepository(database._dbContext),
                Options.Create(new HyperplanningSettings
                {
                    PageUrl = "https://planning.example.org/invite",
                    AcademicYear = AcademicYear,
                    FirstMonday = TestObjects.FirstMonday
                }));
        }

        private ScheduledSession NewSession(int week)
        {
            return new ScheduledSession
            {
                WeekNumber = week,
                Start = new DateTime(2026, 11, 4, 9, 0, 0),
                End = new DateTime(2026, 11, 4, 10, 0, 0),
                Activity = new Activity { Title = "Projets II" }
            };
        }

        [Fact]
        public async Task GetSchedule_GivesEachWeekItsCoverage()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await repository.ReplaceWeekAsync(GroupName, AcademicYear, 7, Array.Empty<ScheduledSession>(), 1, retrievedAtUtc);
            await repository.ReplaceWeekAsync(GroupName, AcademicYear, 8, new[] { NewSession(8) }, 1, retrievedAtUtc);

            var overview = await NewService(database).GetScheduleAsync(GroupName, 7, 9);

            Assert.Equal(new[] { 7, 8, 9 }, overview.Weeks.Select(week => week.Week));
            Assert.Equal(
                new[] { WeekCoverageStatus.VerifiedEmpty, WeekCoverageStatus.Verified, WeekCoverageStatus.Unknown },
                overview.Weeks.Select(week => week.Coverage));
            Assert.Equal(new[] { 0, 1, 0 }, overview.Weeks.Select(week => week.Sessions.Count));
            Assert.Null(overview.Weeks.Last().LastVerifiedAtUtc);
        }

        [Fact]
        public async Task GetSchedule_SessionsWithoutCoverage_AreShownAsUnknown()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await repository.ReplaceWeekAsync(GroupName, AcademicYear, 8, new[] { NewSession(8) }, 1, retrievedAtUtc);
            database._dbContext.ScheduleCoverages.RemoveRange(database._dbContext.ScheduleCoverages);
            await database._dbContext.SaveChangesAsync();

            var overview = await NewService(database).GetScheduleAsync(GroupName, 8, 8);

            var week = Assert.Single(overview.Weeks);
            Assert.Equal(WeekCoverageStatus.Unknown, week.Coverage);
            Assert.Single(week.Sessions);
        }

        [Fact]
        public async Task GetSchedule_GroupNeverImported_Throws()
        {
            using var database = new SqliteTestDatabase();

            await Assert.ThrowsAsync<UnknownGroupException>(() => NewService(database).GetScheduleAsync("XYZ 9", null, null));
        }

        [Fact]
        public async Task GetSchedule_NoWeeksGiven_UsesTheSettingsRange()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await repository.ReplaceWeekAsync(GroupName, AcademicYear, 8, new[] { NewSession(8) }, 1, retrievedAtUtc);

            var overview = await NewService(database).GetScheduleAsync(GroupName, null, null);

            Assert.Equal(Enumerable.Range(1, 14), overview.Weeks.Select(week => week.Week));
        }
    }
}
