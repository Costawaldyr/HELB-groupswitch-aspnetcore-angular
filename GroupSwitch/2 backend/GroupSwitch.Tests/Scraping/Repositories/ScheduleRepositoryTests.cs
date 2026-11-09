using GroupSwitch.Business.Scraping.Models;
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
        private const int FirstWeek = 1;
        private const int SecondWeek = 2;

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

        [Fact]
        public async Task ReplaceGroupWeeks_NewGroup_CreatesGroupAndSessions()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            var count = await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4) });

            Assert.Equal(2, count);
            Assert.Equal(1, await database._dbContext.StudentGroups.CountAsync());
            Assert.Equal(2, await database.CountSessionsOfGroupAsync(IodaGroup));
        }

        [Fact]
        public async Task ReplaceGroupWeeks_SameWeekTwice_ReplacesInsteadOfDuplicating()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4), NewSession("Projet ERP", FirstWeek, 5) });
            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4) });

            Assert.Equal(2, await database.CountSessionsOfGroupAsync(IodaGroup));
            Assert.Equal(1, await database._dbContext.StudentGroups.CountAsync());
        }

        [Fact]
        public async Task ReplaceGroupWeeks_SameTitleTwice_CreatesOneActivity()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", FirstWeek, 3), NewSession("Projets II", FirstWeek, 4) });

            Assert.Equal(1, await database._dbContext.Activities.CountAsync());
        }

        [Fact]
        public async Task ReplaceGroupWeeks_TwoGroupsSameTitle_ShareTheActivity()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek }, new[] { NewSession("Projets II", FirstWeek, 3) });
            await repository.ReplaceGroupWeeksAsync(SiGroup, new[] { FirstWeek }, new[] { NewSession("Projets II", FirstWeek, 4) });

            Assert.Equal(1, await database._dbContext.Activities.CountAsync());
            Assert.Equal(2, await database._dbContext.StudentGroups.CountAsync());
        }

        [Fact]
        public async Task ReplaceGroupWeeks_OneGroup_DoesNotTouchTheOtherGroup()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4) });
            await repository.ReplaceGroupWeeksAsync(SiGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4), NewSession("Projet ERP", FirstWeek, 5) });

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek }, new[] { NewSession("Projets II", FirstWeek, 6) });

            Assert.Equal(1, await database.CountSessionsOfGroupAsync(IodaGroup));
            Assert.Equal(3, await database.CountSessionsOfGroupAsync(SiGroup));
        }

        [Fact]
        public async Task GetSessionsOfGroup_ReturnsSessionsOrderedByStart()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", FirstWeek, 9), NewSession("Projet ERP", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 5) });

            var days = (await repository.GetSessionsOfGroupAsync(IodaGroup))
                .Select(session => session.Start.Day)
                .ToList();

            Assert.Equal(new[] { 3, 5, 9 }, days);
        }

        [Fact]
        public async Task ReplaceGroupWeeks_RealWeekEleven_StoresFifteenSessionsAndElevenActivities()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            var parsed = new WeekScheduleParser().Parse(FixtureReader.Read("week11.json"));
            var sessions = TestObjects.NewSessionBuilder().Build(parsed, week => true);

            var count = await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { 11 }, sessions);

            Assert.Equal(15, count);
            Assert.Equal(11, await database._dbContext.Activities.CountAsync());
        }

        [Fact]
        public async Task ReplaceGroupWeeks_PartialReimport_KeepsSessionsOfOtherWeeks()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek, SecondWeek },
                new[]
                {
                    NewSession("Projets II", FirstWeek, 3), NewSession("Projet ERP", FirstWeek, 4),
                    NewSession("Projets II", SecondWeek, 10), NewSession("Projet ERP", SecondWeek, 11)
                });

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { SecondWeek }, new[] { NewSession("Projets II", SecondWeek, 12) });

            Assert.Equal(2, await database.CountSessionsOfGroupWeekAsync(IodaGroup, FirstWeek));
            Assert.Equal(1, await database.CountSessionsOfGroupWeekAsync(IodaGroup, SecondWeek));
        }

        [Fact]
        public async Task ReplaceGroupWeeks_ScrapedWeekWithoutSessions_ClearsOnlyThatWeek()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek, SecondWeek },
                new[] { NewSession("Projets II", FirstWeek, 3), NewSession("Projets II", SecondWeek, 10) });

            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { SecondWeek }, Enumerable.Empty<ScheduledSession>());

            Assert.Equal(1, await database.CountSessionsOfGroupWeekAsync(IodaGroup, FirstWeek));
            Assert.Equal(0, await database.CountSessionsOfGroupWeekAsync(IodaGroup, SecondWeek));
        }

        [Fact]
        public async Task ReplaceGroupWeeks_SessionOutsideScrapedWeeks_ThrowsAndWritesNothing()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            await Assert.ThrowsAsync<ArgumentException>(() => repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek },
                new[] { NewSession("Projets II", SecondWeek, 3) }));

            Assert.Equal(0, await database._dbContext.StudentGroups.CountAsync());
        }

        [Fact]
        public async Task ReplaceGroupWeeks_NoScrapedWeeks_ChangesNothing()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);

            var count = await repository.ReplaceGroupWeeksAsync(IodaGroup, Array.Empty<int>(), Enumerable.Empty<ScheduledSession>());

            Assert.Equal(0, count);
            Assert.Equal(0, await database._dbContext.StudentGroups.CountAsync());
        }

        [Fact]
        public async Task ReplaceGroupWeeks_SessionWithoutActivity_RollsBackAndKeepsOldSessions()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ScheduleRepository(database._dbContext);
            await repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek }, new[] { NewSession("Projets II", FirstWeek, 3) });
            var invalidSession = new ScheduledSession { WeekNumber = FirstWeek };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                repository.ReplaceGroupWeeksAsync(IodaGroup, new[] { FirstWeek }, new[] { invalidSession }));

            Assert.Equal(1, await database.CountSessionsOfGroupAsync(IodaGroup));
        }
    }
}