using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GroupSwitch.Data.Repositories
{
    public class ImportRunRepository : IImportRunRepository
    {
        private const string UnknownImportFormat = "Import {0} introuvable.";

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
            var run = await _dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .FirstOrDefaultAsync(importRun => importRun.Id == importRunId);

            if (run is null)
            {
                throw new ArgumentException(string.Format(UnknownImportFormat, importRunId));
            }

            return run;
        }

        public async Task<IEnumerable<ImportRun>> GetLatestAsync(int count)
        {
            return await _dbContext.ImportRuns
                .OrderByDescending(run => run.Id)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ImportRun> StartAsync(string? requestedGroupName, string academicYear, int firstWeek, int lastWeek)
        {
            var run = new ImportRun
            {
                StartedAt = DateTime.Now,
                Status = ImportStatus.Running,
                RequestedGroupName = requestedGroupName,
                AcademicYear = academicYear,
                FirstWeek = firstWeek,
                LastWeek = lastWeek
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
                Message = Shorten(message)
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateWeekCountsAsync(int importRunId, int successfulWeeks, int rejectedWeeks)
        {
            var run = await _dbContext.ImportRuns.SingleAsync(importRun => importRun.Id == importRunId);

            run.SuccessfulWeeks = successfulWeeks;
            run.RejectedWeeks = rejectedWeeks;

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

        public async Task<bool> RequestCancellationAsync(int importRunId)
        {
            var run = await GetByIdAsync(importRunId);

            if (run.Status != ImportStatus.Running)
            {
                return false;
            }

            run.CancellationRequested = true;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public Task<bool> IsCancellationRequestedAsync(int importRunId)
        {
            return _dbContext.ImportRuns
                .Where(run => run.Id == importRunId)
                .Select(run => run.CancellationRequested)
                .SingleAsync();
        }

        public async Task<int> RecoverInterruptedAsync(string interruptionMessage, string level)
        {
            var runningRuns = await _dbContext.ImportRuns
                .Where(run => run.Status == ImportStatus.Running)
                .ToListAsync();

            foreach (var run in runningRuns)
            {
                run.Status = ImportStatus.Interrupted;
                run.FinishedAt = DateTime.Now;
                run.LogEntriesList.Add(new ImportLogEntry
                {
                    Moment = DateTime.Now,
                    Level = level,
                    Message = Shorten(interruptionMessage)
                });
            }

            await _dbContext.SaveChangesAsync();

            return runningRuns.Count;
        }

        private string Shorten(string message)
        {
            return message.Length <= ImportLogEntry.MessageMaxLength ? message : message.Substring(0, ImportLogEntry.MessageMaxLength);
        }
    }
}
