using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.EntityFrameworkCore;


namespace GroupSwitch.Tests.Scraping.Repositories
{
    public class ImportRunRepositoryTests
    {
        private const string AcademicYear = "2026-2027";
        private const string InterruptionMessage = "interrompu";
        private const string ErrorLevel = "Error";

        private Task<ImportRun> StartAllWeeksAsync(ImportRunRepository repository)
        {
            return repository.StartAsync(null, AcademicYear, 1, 14);
        }

        [Fact]
        public async Task Start_CreatesRunningRunAndBlocksNewImports()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);

            var run = await StartAllWeeksAsync(repository);

            Assert.Equal(ImportStatus.Running, run.Status);
            Assert.True(await repository.HasRunningImportAsync());
        }

        [Fact]
        public async Task Finish_SavesStatusAndCountsAndFreesTheLock()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var run = await StartAllWeeksAsync(repository);

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
            var run = await StartAllWeeksAsync(repository);

            await repository.AddLogAsync(run.Id, "Info", new string('x', 600));

            var entry = await database._dbContext.ImportLogEntries.SingleAsync();
            Assert.Equal(500, entry.Message.Length);
        }

        [Fact]
        public async Task Start_StoresTheRequestedScope()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);

            var run = await repository.StartAsync("IODA B3", AcademicYear, 8, 11);

            var saved = await database._dbContext.ImportRuns.AsNoTracking().SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal("IODA B3", saved.RequestedGroupName);
            Assert.Equal(AcademicYear, saved.AcademicYear);
            Assert.Equal(8, saved.FirstWeek);
            Assert.Equal(11, saved.LastWeek);
        }

        [Fact]
        public async Task UpdateWeekCounts_SavesSuccessfulAndRejectedWeeks()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var run = await StartAllWeeksAsync(repository);

            await repository.UpdateWeekCountsAsync(run.Id, 12, 2);

            var saved = await database._dbContext.ImportRuns.AsNoTracking().SingleAsync(importRun => importRun.Id == run.Id);
            Assert.Equal(12, saved.SuccessfulWeeks);
            Assert.Equal(2, saved.RejectedWeeks);
        }

        [Fact]
        public async Task RequestCancellation_RunningRun_IsRecorded()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var run = await StartAllWeeksAsync(repository);

            var isRecorded = await repository.RequestCancellationAsync(run.Id);

            Assert.True(isRecorded);
            Assert.True(await repository.IsCancellationRequestedAsync(run.Id));
        }

        [Fact]
        public async Task RequestCancellation_FinishedRun_IsRefused()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var run = await StartAllWeeksAsync(repository);
            await repository.FinishAsync(run.Id, ImportStatus.Finished, 0, 0);

            var isRecorded = await repository.RequestCancellationAsync(run.Id);

            Assert.False(isRecorded);
            Assert.False(await repository.IsCancellationRequestedAsync(run.Id));
        }

        [Fact]
        public async Task RequestCancellation_UnknownRun_Throws()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);

            await Assert.ThrowsAsync<ArgumentException>(() => repository.RequestCancellationAsync(999));
        }

        [Fact]
        public async Task RecoverInterrupted_MarksOnlyRunningRunsAndLogsTheReason()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var finishedRun = await StartAllWeeksAsync(repository);
            await repository.FinishAsync(finishedRun.Id, ImportStatus.Finished, 1, 1);
            var staleRun = await StartAllWeeksAsync(repository);

            var recoveredCount = await repository.RecoverInterruptedAsync(InterruptionMessage, ErrorLevel);

            var saved = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .AsNoTracking()
                .OrderBy(importRun => importRun.Id)
                .ToListAsync();

            Assert.Equal(1, recoveredCount);
            Assert.Equal(ImportStatus.Finished, saved[0].Status);
            Assert.Equal(ImportStatus.Interrupted, saved[1].Status);
            Assert.NotNull(saved[1].FinishedAt);
            Assert.Contains(saved[1].LogEntriesList, entry => entry.Message == InterruptionMessage && entry.Level == ErrorLevel);
            Assert.False(await repository.HasRunningImportAsync());
        }
    }
}
