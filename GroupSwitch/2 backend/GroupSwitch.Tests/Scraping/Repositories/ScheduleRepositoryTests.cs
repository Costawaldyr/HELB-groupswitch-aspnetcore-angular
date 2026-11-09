using GroupSwitch.Business.Scraping.Parsing;
using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GroupSwitch.Tests.Scraping.Repositories
{
    public class ScheduleRepositoryTests
    {
        private const string IodaGroup = "IODA B3";
        private const string SiGroup = "SI 3";
        private const string AcademicYear = "2026-2027";
        private const string OtherAcademicYear = "2027-2028";
        private const int FirstWeek = 1;
        private const int SecondWeek = 2;
        private const int ImportRunId = 1;

        private readonly DateTime retrievedAtUtc = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        private ScheduledSession NewSession(string title, int week, int day)
        {
            return new ScheduledSession
            {
                WeekNumber = week,
                Start = new DateTime(2026, 11, day, 9, 0, 0),
                End = new DateTime(2026, 11, day, 10, 0, 0),
                Activity = new Activity { Title = title }
            };
        }

        private Task<int> ReplaceAsync(ScheduleRepository repository, string groupName, int week, params ScheduledSession[] sessions)
        {
            return repository.ReplaceWeekAsync(groupName, AcademicYear, week, sessions, ImportRunId, retrievedAtUtc);
        }

        [Fact]
        public async Task ReplaceWeek_NewGroup_CreatesGroupAndSessions()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            var count = await ReplaceAsync(repository, IodaGroup, FirstWeek,
                NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4));

            Assert.Equal(2, count);
            Assert.Equal(1, await database._dbContext.StudentGroups.CountAsync());
            Assert.Equal(2, await database.CountSessionsOfGroupAsync(IodaGroup));
        }

        [Fact]
        public async Task ReplaceWeek_SameWeekTwice_ReplacesInsteadOfDuplicating()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await ReplaceAsync(repository, IodaGroup, FirstWeek,
                NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4), NewSession("Projet ERP", FirstWeek, 5));
            await ReplaceAsync(repository, IodaGroup, FirstWeek,
                NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4));

            Assert.Equal(2, await database.CountSessionsOfGroupAsync(IodaGroup));
            Assert.Equal(1, await database._dbContext.StudentGroups.CountAsync());
        }

        [Fact]
        public async Task ReplaceWeek_SameTitleTwice_CreatesOneActivity()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await ReplaceAsync(repository, IodaGroup, FirstWeek,
                NewSession("Projets II", FirstWeek, 3), NewSession("Projets II", FirstWeek, 4));

            Assert.Equal(1, await database._dbContext.Activities.CountAsync());
        }

        [Fact]
        public async Task ReplaceWeek_TwoGroupsSameTitle_ShareTheActivity()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", FirstWeek, 3));
            await ReplaceAsync(repository, SiGroup, FirstWeek, NewSession("Projets II", FirstWeek, 4));

            Assert.Equal(1, await database._dbContext.Activities.CountAsync());
            Assert.Equal(2, await database._dbContext.StudentGroups.CountAsync());
        }

        [Fact]
        public async Task ReplaceWeek_OneGroup_DoesNotTouchTheOtherGroup()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await ReplaceAsync(repository, IodaGroup, FirstWeek,
                NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4));
            await ReplaceAsync(repository, SiGroup, FirstWeek,
                NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4), NewSession("Projet ERP", FirstWeek, 5));

            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", FirstWeek, 6));

            Assert.Equal(1, await database.CountSessionsOfGroupAsync(IodaGroup));
            Assert.Equal(3, await database.CountSessionsOfGroupAsync(SiGroup));
        }

        [Fact]
        public async Task GetSessions_ReturnsSessionsOrderedByStart()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await ReplaceAsync(repository, IodaGroup, FirstWeek,
                NewSession("Projets II", FirstWeek, 9), NewSession("Projet ERP", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 5));

            var days = (await repository.GetSessionsAsync(IodaGroup, AcademicYear, FirstWeek, FirstWeek))
                .Select(session => session.Start.Day)
                .ToList();

            Assert.Equal(new[] { 3, 5, 9 }, days);
        }

        [Fact]
        public async Task ReplaceWeek_RealWeekEleven_StoresFifteenSessionsAndElevenActivities()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            var parsed = new WeekScheduleParser().Parse(FixtureReader.Read("week11.json"));
            var sessions = TestObjects.NewSessionBuilder().Build(parsed, week => true);

            var count = await repository.ReplaceWeekAsync(IodaGroup, AcademicYear, 11, sessions, ImportRunId, retrievedAtUtc);

            Assert.Equal(15, count);
            Assert.Equal(11, await database._dbContext.Activities.CountAsync());
        }

        [Fact]
        public async Task ReplaceWeek_PartialReimport_KeepsSessionsOfOtherWeeks()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4));
            await ReplaceAsync(repository, IodaGroup, SecondWeek, NewSession("Projets II", SecondWeek, 10), NewSession("Projet ERP", SecondWeek, 11));

            await ReplaceAsync(repository, IodaGroup, SecondWeek, NewSession("Projets II", SecondWeek, 12));

            Assert.Equal(2, await database.CountSessionsOfGroupWeekAsync(IodaGroup, FirstWeek));
            Assert.Equal(1, await database.CountSessionsOfGroupWeekAsync(IodaGroup, SecondWeek));
        }

        [Fact]
        public async Task ReplaceWeek_ScrapedWeekWithoutSessions_ClearsOnlyThatWeek()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", FirstWeek, 3));
            await ReplaceAsync(repository, IodaGroup, SecondWeek, NewSession("Projets II", SecondWeek, 10));

            await ReplaceAsync(repository, IodaGroup, SecondWeek);

            Assert.Equal(1, await database.CountSessionsOfGroupWeekAsync(IodaGroup, FirstWeek));
            Assert.Equal(0, await database.CountSessionsOfGroupWeekAsync(IodaGroup, SecondWeek));
        }

        [Fact]
        public async Task ReplaceWeek_SessionOfAnotherWeek_ThrowsAndWritesNothing()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await Assert.ThrowsAsync<ArgumentException>(() => ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", SecondWeek, 3)));

            Assert.Equal(0, await database._dbContext.StudentGroups.CountAsync());
        }

        [Fact]
        public async Task ReplaceWeek_SessionWithoutActivity_RollsBackAndKeepsOldSessions()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", FirstWeek, 3));
            var invalidSession = new ScheduledSession { WeekNumber = FirstWeek };

            await Assert.ThrowsAsync<ArgumentException>(() => ReplaceAsync(repository, IodaGroup, FirstWeek, invalidSession));

            Assert.Equal(1, await database.CountSessionsOfGroupAsync(IodaGroup));
        }

        [Fact]
        public async Task ReplaceWeek_WeekEight_DoesNotTouchWeekNineNorAnotherGroup()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await ReplaceAsync(repository, IodaGroup, 8, NewSession("Projets II", 8, 3), NewSession("Projet ERP", 8, 4));
            await ReplaceAsync(repository, IodaGroup, 9, NewSession("Projets II", 9, 10), NewSession("Projet ERP", 9, 11));
            await ReplaceAsync(repository, SiGroup, 8, NewSession("Projets II", 8, 3));

            await ReplaceAsync(repository, IodaGroup, 8, NewSession("Projets II", 8, 5));

            Assert.Equal(1, await database.CountSessionsOfGroupWeekAsync(IodaGroup, 8));
            Assert.Equal(2, await database.CountSessionsOfGroupWeekAsync(IodaGroup, 9));
            Assert.Equal(1, await database.CountSessionsOfGroupWeekAsync(SiGroup, 8));
        }

        [Fact]
        public async Task ReplaceWeek_DoesNotTouchSessionsOfAnotherAcademicYear()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await repository.ReplaceWeekAsync(IodaGroup, OtherAcademicYear, FirstWeek, new[] { NewSession("Projets II", FirstWeek, 3) }, ImportRunId, retrievedAtUtc);

            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projet ERP", FirstWeek, 4));

            Assert.Equal(2, await database.CountSessionsOfGroupWeekAsync(IodaGroup, FirstWeek));
            Assert.Equal(1, await database._dbContext.ScheduledSessions.CountAsync(session => session.AcademicYear == OtherAcademicYear));
        }

        [Fact]
        public async Task ReplaceWeek_SetsTheAcademicYearOfTheSessions()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", FirstWeek, 3));

            var session = await database._dbContext.ScheduledSessions.SingleAsync();
            Assert.Equal(AcademicYear, session.AcademicYear);
        }

        [Fact]
        public async Task ReplaceWeek_Twice_KeepsOneCoverageWithTheLatestValues()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            var laterRetrieval = retrievedAtUtc.AddHours(1);

            await ReplaceAsync(repository, IodaGroup, FirstWeek, NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4));
            await repository.ReplaceWeekAsync(IodaGroup, AcademicYear, FirstWeek, new[] { NewSession("Projets II", FirstWeek, 3) }, 2, laterRetrieval);

            var coverage = await database._dbContext.ScheduleCoverages.SingleAsync();
            Assert.Equal(FirstWeek, coverage.WeekNumber);
            Assert.Equal(1, coverage.SessionCount);
            Assert.Equal(2, coverage.LastImportRunId);
            Assert.Equal(laterRetrieval, coverage.LastVerifiedAtUtc);
        }

        [Fact]
        public async Task ReplaceWeek_EmptyWeek_RecordsAVerifiedCoverageWithoutSessions()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await ReplaceAsync(repository, IodaGroup, 7);

            var coverage = await database._dbContext.ScheduleCoverages.SingleAsync();
            Assert.Equal(7, coverage.WeekNumber);
            Assert.Equal(0, coverage.SessionCount);
        }

        [Fact]
        public async Task GetCoverages_ReturnsOnlyTheRequestedGroupYearAndWeeks()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await ReplaceAsync(repository, IodaGroup, 7);
            await ReplaceAsync(repository, IodaGroup, 8, NewSession("Projets II", 8, 3));
            await ReplaceAsync(repository, IodaGroup, 12, NewSession("Projets II", 12, 3));
            await ReplaceAsync(repository, SiGroup, 8, NewSession("Projets II", 8, 3));
            await repository.ReplaceWeekAsync(IodaGroup, OtherAcademicYear, 8, Array.Empty<ScheduledSession>(), ImportRunId, retrievedAtUtc);

            var coverages = await repository.GetCoveragesAsync(IodaGroup, AcademicYear, 7, 11);

            Assert.Equal(new[] { 7, 8 }, coverages.Select(coverage => coverage.WeekNumber));
        }
    }
}
