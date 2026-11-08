using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping;
using Microsoft.EntityFrameworkCore;


namespace GroupSwitch.Tests.Repositories
{
    public class ImportRunRepositoryTests
    {
        [Fact]
        public async Task Start_CreatesRunningRunAndBlocksNewImports()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);

            var run = await repository.StartAsync();

            Assert.Equal(ImportStatus.Running, run.Status);
            Assert.True(await repository.HasRunningImportAsync());
        }

        [Fact]
        public async Task Finish_SavesStatusAndCountsAndFreesTheLock()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var run = await repository.StartAsync();

            await repository.FinishAsync(run.Id, ImportStatus.Finished, 78, 1200);

            var saved = await database._dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(ImportStatus.Finished, saved.Status);
            Assert.Equal(78, saved.GroupsImported);
            Assert.Equal(1200, saved.SessionsImported);
            Assert.False(await repository.HasRunningImportAsync());
        }

        [Fact]
        public async Task AddLog_LongMessage_IsCutToFiveHundredCharacters()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var run = await repository.StartAsync();

            await repository.AddLogAsync(run.Id, "Info", new string('x', 600));

            var entry = await database._dbContext.ImportLogEntries.SingleAsync();
            Assert.Equal(500, entry.Message.Length);
        }
    }
}
