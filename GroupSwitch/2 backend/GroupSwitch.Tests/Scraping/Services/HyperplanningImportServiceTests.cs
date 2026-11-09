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
        private readonly string week8Json = FixtureReader.Read("week8.json");
        private readonly string week11Json = FixtureReader.Read("week11.json");

        private HyperplanningImportService NewService(SqliteTestDatabase database, IHyperplanningScraper scraper)
        {
            var importRunRepository = new ImportRunRepository(database._dbContext);

            return new HyperplanningImportService(
                scraper,
                TestObjects.NewWeekAssessmentService(),
                new GroupScheduleCollectorService(),
                new ScheduleRepository(database._dbContext),
                importRunRepository,
                new ImportLogService(importRunRepository),
                Options.Create(new HyperplanningSettings
                {
                    PageUrl = "https://planning.example.org/invite",
                    AcademicYear = "2026-2027",
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
            var run = await service.StartAsync();

            await service.RunAsync(run.Id, name => true, week => true);

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

            var first = await service.StartAsync();
            await service.RunAsync(first.Id, name => true, week => true);
            var second = await service.StartAsync();
            await service.RunAsync(second.Id, name => true, week => true);

            Assert.Equal(39, await database._dbContext.ScheduledSessions.CountAsync());
            Assert.Equal(2, await database._dbContext.ImportRuns.CountAsync());
        }

        [Fact]
        public async Task Run_OnlyWeekEleven_FiltersWithTheDelegate()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync();

            await service.RunAsync(run.Id, name => true, week => week == 11);

            var saved = await database._dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(30, saved.SessionsImported);
        }

        [Fact]
        public async Task Run_ScraperFails_MarksFailedAndKeepsGroupsAlreadySaved()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses(), true));
            var run = await service.StartAsync();

            await service.RunAsync(run.Id, name => true, week => true);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .SingleAsync(importRun => importRun.Id == run.Id);

            Assert.Equal(ImportStatus.Failed, saved.Status);
            Assert.Equal(1, saved.GroupsImported);
            Assert.Equal(24, saved.SessionsImported);
            Assert.Contains(saved.LogEntriesList, entry => entry.Level == ImportLogLevels.Error);
            Assert.Equal(0, await database._dbContext.ScheduledSessions.CountAsync(session => session.StudentGroup!.Name == "SI 3"));
        }

        [Fact]
        public async Task Run_PartialReimport_KeepsTheOtherWeeksOfTheGroup()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            var first = await service.StartAsync();
            await service.RunAsync(first.Id, name => true, week => true);
            var second = await service.StartAsync();
            await service.RunAsync(second.Id, name => true, week => week == 11);

            Assert.Equal(24, await database.CountSessionsOfGroupAsync("IODA B3"));
            Assert.Equal(9, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
        }

        [Fact]
        public async Task Run_InvalidWeek_KeepsItsOldSessionsAndLogsTheStatus()
        {
            using var database = new SqliteTestDatabase();
            var firstService = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var firstRun = await firstService.StartAsync();
            await firstService.RunAsync(firstRun.Id, name => true, week => true);

            var brokenWeek8 = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{}}}".Replace('\'', '"');
            var secondService = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 8, brokenWeek8),
                WeekRetrieval.Succeeded("IODA B3", 11, week11Json)
            }));
            var secondRun = await secondService.StartAsync();

            await secondService.RunAsync(secondRun.Id, name => true, week => true);

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
            var firstRun = await firstService.StartAsync();
            await firstService.RunAsync(firstRun.Id, name => true, week => true);

            var emptyWeek8 = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{'ListeCours':[],'ListeAnnulationsCours':[]}}}".Replace('\'', '"');
            var secondService = NewService(database, new FakeHyperplanningScraper(new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 8, emptyWeek8)
            }));
            var secondRun = await secondService.StartAsync();

            await secondService.RunAsync(secondRun.Id, name => true, week => true);

            Assert.Equal(0, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
            Assert.Equal(15, await database.CountSessionsOfGroupWeekAsync("IODA B3", 11));
            Assert.Equal(15, await database.CountSessionsOfGroupAsync("SI 3"));
        }

        [Fact]
        public async Task Start_WhileAnotherImportRuns_Throws()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));

            await service.StartAsync();

            await Assert.ThrowsAsync<ImportAlreadyRunningException>(() => service.StartAsync());
        }
    }
}
