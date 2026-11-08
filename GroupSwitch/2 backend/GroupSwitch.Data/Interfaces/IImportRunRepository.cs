using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Enums;


namespace GroupSwitch.Data.Interfaces
{
    public interface IImportRunRepository
    {
        Task<bool> HasRunningImportAsync();

        Task<ImportRun> StartAsync();

        Task AddLogAsync(int importRunId, string level, string message);

        Task FinishAsync(int importRunId, ImportStatus status, int groupsImported, int sessionsImported);
        Task<ImportRun> GetByIdAsync(int importRunId);
        Task<IEnumerable<ImportRun>> GetLatestAsync(int count);
    }
}
