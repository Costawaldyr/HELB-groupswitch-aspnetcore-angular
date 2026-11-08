using GroupSwitch.Data.Entities;
using Microsoft.EntityFrameworkCore;
using GroupSwitch.Data.Interfaces;
using GroupSwitch.Data.Enums;


namespace GroupSwitch.Data.Repositories
{
    public class ImportRunRepository : IImportRunRepository
    {
        private const int MessageMaxLength = 500;

        private readonly GroupSwitchDbContext _dbContext;

        public ImportRunRepository(GroupSwitchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> HasRunningImportAsync()
        {
            return await _dbContext.ImportRuns.AnyAsync(run => run.Status == ImportStatus.Running);
        }

        public async Task<ImportRun> GetByIdAsync(int importRunId)
        {
            var runs = await _dbContext.ImportRuns
                .Include(run => run.LogEntriesList)
                .Where(run => run.Id == importRunId)
                .ToListAsync();

            if (!runs.Any())
            {
                throw new ArgumentException($"Import {importRunId} introuvable.");
            }

            return runs.First();
        }

        public async Task<IEnumerable<ImportRun>> GetLatestAsync(int count)
        {
            return await _dbContext.ImportRuns
                .OrderByDescending(run => run.Id)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ImportRun> StartAsync()
        {
            var run = new ImportRun
            {
                StartedAt = DateTime.Now,
                Status = ImportStatus.Running
            };

            _dbContext.ImportRuns.Add(run);
            await _dbContext.SaveChangesAsync();

            return run;
        }

        public async Task AddLogAsync(int importRunId, string level, string message)
        {
            _dbContext.ImportLogEntries.Add(new ImportLogEntry
            {
                ImportRunId = importRunId,
                Moment = DateTime.Now,
                Level = level,
                Message = message.Length <= MessageMaxLength ? message : message.Substring(0, MessageMaxLength)
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task FinishAsync(int importRunId, ImportStatus status, int groupsImported, int sessionsImported)
        {
            var run = await _dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == importRunId);

            run.Status = status;
            run.FinishedAt = DateTime.Now;
            run.GroupsImported = groupsImported;
            run.SessionsImported = sessionsImported;

            await _dbContext.SaveChangesAsync();
        }
    }
}
