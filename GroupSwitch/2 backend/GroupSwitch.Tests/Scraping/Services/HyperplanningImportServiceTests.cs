using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Services;
using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace GroupSwitch.Tests.Scraping.Services
{
    public class HyperplanningImportServiceTests
    {
        private const string AcademicYear = "2026-2027";

        private readonly string week8Json = FixtureReader.Read("week8.json");
        private readonly string week11Json = FixtureReader.Read("week11.json");
        private readonly ImportRequest allGroupsAllWeeks = new(null, null, null);

        private HyperplanningImportService NewService(SqliteTestDatabase database, IHyperplanningScraper scraper)
        {
            var importRunRepository = new ImportRunRepository(database._dbContext);

            return new HyperplanningImportService(
                scraper,
                TestObjects.NewWeekAssessmentService(),
                new ScheduleRepository(database._dbContext),
                importRunRepository,
                new ImportLogService(importRunRepository),
                Options.Create(new HyperplanningSettings
                {
                    PageUrl = "https://planning.example.org/invite",
                    AcademicYear = AcademicYear,
                    FirstMonday = TestObjects.FirstMonday
                }));
        }

        private List<WeekRetrieval> TwoGroupsResponses()
        {
            return new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 8, week8Json),
                WeekRetrieval.Succeeded("IODA B3", 11, week11Json),
                WeekRetrieval.Succeeded("SI 3", 11, week11Json)
            };
        }

        [Fact]
        public async Task Run_TwoGroups_StoresAllSessionsAndFinishes()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .SingleAsync(importRun => importRun.Id == run.Id);

            Assert.Equal(ImportStatus.Finished, saved.Status);
            Assert.Equal(2, saved.GroupsImported);
            Assert.Equal(39, saved.SessionsImported);
            Assert.Equal(4, saved.LogEntriesList.Count);
            Assert.Equal(24, await database._dbContext.ScheduledSessions.CountAsync(session => session.StudentGroup!.Name == "IODA B3"));
            Assert.Equal(15, await database._dbContext.ScheduledSessions.CountAsync(session => session.StudentGroup!.Name == "SI 3"));
        }

        [Fact]
        public async Task Run_Twice_GivesTheSameResult()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            var first = await service.StartAsync(allGroupsAllWeeks);
            await service.RunAsync(first.Id);
            var second = await service.StartAsync(allGroupsAllWeeks);
            await service.RunAsync(second.Id);

            Assert.Equal(39, await database._dbContext.ScheduledSessions.CountAsync());
            Assert.Equal(2, await database._dbContext.ImportRuns.CountAsync());
        }

        [Fact]
        public async Task Run_OnlyWeekEleven_FiltersWithTheDelegate()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(new ImportRequest(null, 11, 11));

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(30, saved.SessionsImported);
        }

        [Fact]
        public async Task Run_ScraperFailsAfterTheLastWeek_IsFailedButKeepsEveryWeekAlreadyVerified()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses(), true));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .SingleAsync(importRun => importRun.Id == run.Id);

            Assert.Equal(ImportStatus.Failed, saved.Status);
            Assert.Equal(2, saved.GroupsImported);
            Assert.Equal(39, saved.SessionsImported);
            Assert.Contains(saved.LogEntriesList, entry => entry.Level == ImportLogLevels.Error);
            Assert.Equal(15, await database._dbContext.ScheduledSessions.CountAsync(session => session.StudentGroup!.Name == "SI 3"));
        }

        [Fact]
        public async Task Run_ScraperFailsBeforeTheLastGroup_IsFailedAndSavesOnlyTheWeeksReceived()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses(), failAfterRetrievalCount: 2));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .SingleAsync(importRun => importRun.Id == run.Id);

            Assert.Equal(ImportStatus.Failed, saved.Status);
            Assert.Equal(1, saved.GroupsImported);
            Assert.Equal(24, saved.SessionsImported);
            Assert.Contains(saved.LogEntriesList, entry => entry.Level == ImportLogLevels.Error);
            Assert.Equal(24, await database.CountSessionsOfGroupAsync("IODA B3"));
            Assert.Equal(0, await database.CountSessionsOfGroupAsync("SI 3"));
        }

        [Fact]
        public async Task Run_PartialReimport_KeepsTheOtherWeeksOfTheGroup()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            var first = await service.StartAsync(allGroupsAllWeeks);
            await service.RunAsync(first.Id);
            var second = await service.StartAsync(new ImportRequest(null, 11, 11));
            await service.RunAsync(second.Id);

            Assert.Equal(24, await database.CountSessionsOfGroupAsync("IODA B3"));
            Assert.Equal(9, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
        }

        [Fact]
        public async Task Run_InvalidWeek_KeepsItsOldSessionsAndLogsTheStatus()
        {
            using var database = new SqliteTestDatabase();
            var firstService = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var firstRun = await firstService.StartAsync(allGroupsAllWeeks);
            await firstService.RunAsync(firstRun.Id);

            var brokenWeek8 = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{}}}".Replace('\'', '"');
            var secondService = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 8, brokenWeek8),
                WeekRetrieval.Succeeded("IODA B3", 11, week11Json)
            }));
            var secondRun = await secondService.StartAsync(allGroupsAllWeeks);

            await secondService.RunAsync(secondRun.Id);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .SingleAsync(importRun => importRun.Id == secondRun.Id);

            Assert.Equal(9, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
            Assert.Contains(saved.LogEntriesList, entry => entry.Level == ImportLogLevels.Error && entry.Message.Contains("Invalid"));
        }

        [Fact]
        public async Task Run_VerifiedEmptyWeek_RemovesOnlyItsOwnSessions()
        {
            using var database = new SqliteTestDatabase();
            var firstService = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var firstRun = await firstService.StartAsync(allGroupsAllWeeks);
            await firstService.RunAsync(firstRun.Id);

            var emptyWeek8 = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{'ListeCours':[],'ListeAnnulationsCours':[]}}}".Replace('\'', '"');
            var secondService = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 8, emptyWeek8)
            }));
            var secondRun = await secondService.StartAsync(allGroupsAllWeeks);

            await secondService.RunAsync(secondRun.Id);

            Assert.Equal(0, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
            Assert.Equal(15, await database.CountSessionsOfGroupWeekAsync("IODA B3", 11));
            Assert.Equal(15, await database.CountSessionsOfGroupAsync("SI 3"));
        }

        [Fact]
        public async Task Start_WhileAnotherImportRuns_Throws()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            await service.StartAsync(allGroupsAllWeeks);

            await Assert.ThrowsAsync<ImportAlreadyRunningException>(() => service.StartAsync(allGroupsAllWeeks));
        }

        [Fact]
        public async Task Run_OneWeekTimesOut_IsPartiallyCompletedAndSavesTheOtherWeeks()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 8, week8Json),
                WeekRetrieval.Failed("IODA B3", 11, "Timeout"),
                WeekRetrieval.Succeeded("SI 3", 11, week11Json)
            }));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .SingleAsync(importRun => importRun.Id == run.Id);

            Assert.Equal(ImportStatus.PartiallyCompleted, saved.Status);
            Assert.Equal(2, saved.SuccessfulWeeks);
            Assert.Equal(1, saved.RejectedWeeks);
            Assert.Equal(9, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
            Assert.Equal(0, await database.CountSessionsOfGroupWeekAsync("IODA B3", 11));
            Assert.Equal(15, await database.CountSessionsOfGroupAsync("SI 3"));
            Assert.Contains(saved.LogEntriesList, entry => entry.Message.Contains("RetrievalFailed") && entry.Message.Contains("Timeout"));
        }

        [Fact]
        public async Task Run_OneWeekTimesOut_KeepsTheOldSessionsOfThatWeek()
        {
            using var database = new SqliteTestDatabase();
            var firstService = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var firstRun = await firstService.StartAsync(allGroupsAllWeeks);
            await firstService.RunAsync(firstRun.Id);

            var secondService = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Failed("IODA B3", 11, "Timeout")
            }));
            var secondRun = await secondService.StartAsync(allGroupsAllWeeks);

            await secondService.RunAsync(secondRun.Id);

            Assert.Equal(15, await database.CountSessionsOfGroupWeekAsync("IODA B3", 11));
        }

        [Fact]
        public async Task Run_ThreeGroupsInARowFailEntirely_StopsWithFailedAndExplainsWhy()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Failed("ES B2", 8, "Timeout"),
                WeekRetrieval.Failed("ES B2", 11, "Timeout"),
                WeekRetrieval.Failed("ES B3", 8, "Timeout"),
                WeekRetrieval.Failed("ES B3", 11, "Timeout"),
                WeekRetrieval.Failed("AS J B1", 8, "Timeout"),
                WeekRetrieval.Failed("AS J B1", 11, "Timeout"),
                WeekRetrieval.Succeeded("IODA B3", 8, week8Json)
            }));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .SingleAsync(importRun => importRun.Id == run.Id);

            Assert.Equal(ImportStatus.Failed, saved.Status);
            Assert.Equal(6, saved.RejectedWeeks);
            Assert.Equal(0, await database.CountSessionsOfGroupAsync("IODA B3"));
            Assert.Contains(saved.LogEntriesList, entry => entry.Level == ImportLogLevels.Error && entry.Message.Contains("3 groupes de suite"));
        }

        [Fact]
        public async Task Run_FailedGroupsSeparatedByASuccessfulGroup_DoNotStopTheImport()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Failed("ES B2", 8, "Timeout"),
                WeekRetrieval.Failed("ES B3", 8, "Timeout"),
                WeekRetrieval.Succeeded("IODA B3", 8, week8Json),
                WeekRetrieval.Failed("AS J B1", 8, "Timeout"),
                WeekRetrieval.Failed("AS J B2", 8, "Timeout"),
                WeekRetrieval.Succeeded("SI 3", 11, week11Json)
            }));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(ImportStatus.PartiallyCompleted, saved.Status);
            Assert.Equal(9, await database.CountSessionsOfGroupAsync("IODA B3"));
            Assert.Equal(15, await database.CountSessionsOfGroupAsync("SI 3"));
        }

        [Fact]
        public async Task Run_DatabaseRefusesTheSessions_IsFailedWithAMessageAndFreesTheNextImport()
        {
            using var database = new SqliteTestDatabase();
            await database._dbContext.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER RefuseSessions BEFORE INSERT ON ScheduledSessions BEGIN SELECT RAISE(ABORT, 'forced insert failure'); END;");
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .AsNoTracking()
                .SingleAsync(importRun => importRun.Id == run.Id);

            Assert.Equal(ImportStatus.Failed, saved.Status);
            Assert.NotNull(saved.FinishedAt);
            Assert.Contains(saved.LogEntriesList, entry => entry.Level == ImportLogLevels.Error && entry.Message.Contains("forced insert failure"));
            Assert.Equal(0, await database._dbContext.ScheduledSessions.CountAsync());

            var nextRun = await service.StartAsync(allGroupsAllWeeks);
            Assert.Equal(ImportStatus.Running, nextRun.Status);
        }

        [Fact]
        public async Task FailIfStillRunning_RunningRun_BecomesFailedWithTheReason()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(allGroupsAllWeeks);

            var isMarked = await service.FailIfStillRunningAsync(run.Id, "disk full");

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .AsNoTracking()
                .SingleAsync(importRun => importRun.Id == run.Id);
            Assert.True(isMarked);
            Assert.Equal(ImportStatus.Failed, saved.Status);
            Assert.Contains(saved.LogEntriesList, entry => entry.Message.Contains("disk full"));
        }

        [Fact]
        public async Task FailIfStillRunning_FinishedRun_IsLeftUnchanged()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(allGroupsAllWeeks);
            await service.RunAsync(run.Id);

            var isMarked = await service.FailIfStillRunningAsync(run.Id, "late failure");

            var saved = await database._dbContext.ImportRuns.AsNoTracking().SingleAsync(importRun => importRun.Id == run.Id);
            Assert.False(isMarked);
            Assert.Equal(ImportStatus.Finished, saved.Status);
        }

        [Fact]
        public async Task Run_NoWeekAccepted_IsFailed()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Failed("IODA B3", 8, "Timeout"),
                WeekRetrieval.Failed("IODA B3", 11, "Timeout")
            }));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(ImportStatus.Failed, saved.Status);
            Assert.Equal(0, saved.SuccessfulWeeks);
            Assert.Equal(2, saved.RejectedWeeks);
        }

        [Fact]
        public async Task Start_UnknownGroup_ThrowsAndCreatesNoRun()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            await Assert.ThrowsAsync<UnknownGroupException>(() => service.StartAsync(new ImportRequest("XYZ 9", null, null)));

            Assert.Equal(0, await database._dbContext.ImportRuns.CountAsync());
        }

        [Theory]
        [InlineData(0, 14)]
        [InlineData(1, 15)]
        [InlineData(9, 8)]
        public async Task Start_WeeksOutsideTheSettings_ThrowsAndCreatesNoRun(int firstWeek, int lastWeek)
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            await Assert.ThrowsAsync<InvalidWeekRangeException>(() => service.StartAsync(new ImportRequest(null, firstWeek, lastWeek)));

            Assert.Equal(0, await database._dbContext.ImportRuns.CountAsync());
        }

        [Fact]
        public async Task Start_KnownGroup_StoresTheRequestedScope()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            var run = await service.StartAsync(new ImportRequest("SI 3", 8, 11));

            Assert.Equal("SI 3", run.RequestedGroupName);
            Assert.Equal(AcademicYear, run.AcademicYear);
            Assert.Equal(8, run.FirstWeek);
            Assert.Equal(11, run.LastWeek);
        }

        [Fact]
        public async Task Run_RequestedGroup_ImportsOnlyThatGroup()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(new ImportRequest("SI 3", null, null));

            await service.RunAsync(run.Id);

            Assert.Equal(15, await database.CountSessionsOfGroupAsync("SI 3"));
            Assert.Equal(0, await database.CountSessionsOfGroupAsync("IODA B3"));
        }

        [Fact]
        public async Task Run_CancellationRequested_StopsAfterTheCurrentWeekAndIsCancelled()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(allGroupsAllWeeks);
            await service.RequestCancellationAsync(run.Id);

            await service.RunAsync(run.Id);

            var saved = await database._dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(ImportStatus.Cancelled, saved.Status);
            Assert.Equal(9, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
            Assert.Equal(0, await database.CountSessionsOfGroupWeekAsync("IODA B3", 11));
            Assert.Equal(0, await database.CountSessionsOfGroupAsync("SI 3"));
        }

        [Fact]
        public async Task Run_ApplicationStopping_IsInterrupted()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync(allGroupsAllWeeks);
            using var stopping = new CancellationTokenSource();
            stopping.Cancel();

            await service.RunAsync(run.Id, stopping.Token);

            var saved = await database._dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(ImportStatus.Interrupted, saved.Status);
            Assert.Equal(0, await database._dbContext.ScheduledSessions.CountAsync());
        }

        [Fact]
        public async Task RecoverInterrupted_RunningRun_BecomesInterruptedAndAllowsANewImport()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var staleRun = await service.StartAsync(allGroupsAllWeeks);

            var recoveredCount = await service.RecoverInterruptedAsync();
            var newRun = await service.StartAsync(allGroupsAllWeeks);

            var saved = await database._dbContext.ImportRuns.AsNoTracking().SingleAsync(importRun => importRun.Id == staleRun.Id);
            Assert.Equal(1, recoveredCount);
            Assert.Equal(ImportStatus.Interrupted, saved.Status);
            Assert.Equal(ImportStatus.Running, newRun.Status);
        }

        [Fact]
        public async Task Run_SavedWeeks_RecordTheirCoverage()
        {
            using var database = new SqliteTestDatabase();
            var emptyWeek7 = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{'ListeCours':[],'ListeAnnulationsCours':[]}}}".Replace('\'', '"');
            var service = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 7, emptyWeek7),
                WeekRetrieval.Succeeded("IODA B3", 8, week8Json)
            }));
            var run = await service.StartAsync(allGroupsAllWeeks);

            await service.RunAsync(run.Id);

            var coverages = await database._dbContext.ScheduleCoverages
                .OrderBy(coverage => coverage.WeekNumber)
                .ToListAsync();

            Assert.Equal(new[] { 7, 8 }, coverages.Select(coverage => coverage.WeekNumber));
            Assert.Equal(new[] { 0, 9 }, coverages.Select(coverage => coverage.SessionCount));
            Assert.All(coverages, coverage => Assert.Equal(run.Id, coverage.LastImportRunId));
            Assert.All(coverages, coverage => Assert.Equal(AcademicYear, coverage.AcademicYear));
        }
    }
}
