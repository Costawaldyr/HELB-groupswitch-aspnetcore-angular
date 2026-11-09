using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Enums;

namespace GroupSwitch.Data.Interfaces
{
    public interface IImportRunRepository
    {
        Task<bool> HasRunningImportAsync();

        Task<ImportRun> StartAsync(string? requestedGroupName, string academicYear, int firstWeek, int lastWeek);

        Task AddLogAsync(int importRunId, string level, string message);

        Task UpdateWeekCountsAsync(int importRunId, int successfulWeeks, int rejectedWeeks);

        Task FinishAsync(int importRunId, ImportStatus status, int groupsImported, int sessionsImported);

        Task<ImportRun> GetByIdAsync(int importRunId);

        Task<IEnumerable<ImportRun>> GetLatestAsync(int count);

        Task<bool> RequestCancellationAsync(int importRunId);

        Task<bool> IsCancellationRequestedAsync(int importRunId);

        Task<int> RecoverInterruptedAsync(string interruptionMessage, string level);
    }
}
