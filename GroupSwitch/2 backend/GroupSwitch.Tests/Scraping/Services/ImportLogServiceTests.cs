using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Services;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GroupSwitch.Tests.Scraping.Services
{
    public class ImportLogServiceTests
    {
        [Fact]
        public async Task InfoAndError_WriteEntriesWithTheRightLevels()
        {
            using var database = new SqliteTestDatabase();
            var repository = new ImportRunRepository(database._dbContext);
            var importLogService = new ImportLogService(repository);
            var run = await repository.StartAsync(null, "2026-2027", 1, 14);

            await importLogService.InfoAsync(run.Id, "premier");
            await importLogService.ErrorAsync(run.Id, "second");

            var levels = await database._dbContext.ImportLogEntries
                .OrderBy(entry => entry.Id)
                .Select(entry => entry.Level)
                .ToListAsync();

            Assert.Equal(new[] { ImportLogLevels.Info, ImportLogLevels.Error }, levels);
        }
    }
}