using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;
using GroupSwitch.Business.Services;
using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping;
using GroupSwitch.Tests.Utils;
using Microsoft.EntityFrameworkCore;


namespace GroupSwitch.Tests.Services
{
    public class ImportServiceTests
    {
        private readonly string week8Json = FixtureReader.Read("week8.json");
        private readonly string week11Json = FixtureReader.Read("week11.json");

        private ImportService NewService(SqliteTestDatabase database, IHyperplanningScraper scraper)
        {
            var importRunRepository = new ImportRunRepository(database._dbContext);

            return new ImportService(
                scraper,
                new GroupScheduleCollector(TestObjects.NewConverter()),
                new ScheduleRepository(database._dbContext),
                importRunRepository,
                new ImportJournal(importRunRepository));
        }
        private List<WeekResponse> TwoGroupsResponses()
        {
            return new List<WeekResponse>
            {
                new("IODA B3", 8, week8Json),
                new("IODA B3", 11, week11Json),
                new("SI 3", 11, week11Json)
            };
        }

        [Fact]
        public async Task Run_TwoGroups_StoresAllSessionsAndFinishes()
        {
            using var database = new SqliteTestDatabase();
            var service = NewService(database, new FakeHyperplanningScraper(TwoGroupsResponses()));
            var run = await service.StartAsync();

            await service.RunAsync(run.Id, name => true, QuadrimesterFilters.IsInFirstQuadrimester);

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
            await service.RunAsync(first.Id, name => true, QuadrimesterFilters.IsInFirstQuadrimester);
            var second = await service.StartAsync();
            await service.RunAsync(second.Id, name => true, QuadrimesterFilters.IsInFirstQuadrimester);

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

            await service.RunAsync(run.Id, name => true, QuadrimesterFilters.IsInFirstQuadrimester);

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
            await service.RunAsync(first.Id, name => true, QuadrimesterFilters.IsInFirstQuadrimester);
            var second = await service.StartAsync();
            await service.RunAsync(second.Id, name => true, week => week == 11);

            Assert.Equal(24, await database.CountSessionsOfGroupAsync("IODA B3"));
            Assert.Equal(9, await database.CountSessionsOfGroupWeekAsync("IODA B3", 8));
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
