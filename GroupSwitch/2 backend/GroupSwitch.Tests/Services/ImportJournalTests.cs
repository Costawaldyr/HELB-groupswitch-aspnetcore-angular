using GroupSwitch.Business.Models;
using GroupSwitch.Business.Services;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping;
using Microsoft.EntityFrameworkCore;

namespace GroupSwitch.Tests.Services
{
    public class ImportJournalTests
    {
        [Fact]
        public async Task InfoAndError_WriteEntriesWithTheRightLevels()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var journal = new ImportJournal(repository);
            var run = await repository.StartAsync();

            await journal.InfoAsync(run.Id, "premier");
            await journal.ErrorAsync(run.Id, "second");

            var levels = await database._dbContext.ImportLogEntries
                .OrderBy(entry => entry.Id)
                .Select(entry => entry.Level)
                .ToListAsync();

            Assert.Equal(new[] { ImportLogLevels.Info, ImportLogLevels.Error }, levels);
        }
    }
}