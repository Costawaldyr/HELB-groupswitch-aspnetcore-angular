using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IHyperplanningImportService
    {
        Task<ImportRun> StartAsync(ImportRequest request, CancellationToken cancellationToken = default);
        Task RunAsync(int importRunId, CancellationToken cancellationToken = default);
        Task<ImportRun> GetAsync(int importRunId);
        Task<IEnumerable<ImportRun>> GetLatestAsync(int count);
        Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken cancellationToken = default);
        Task<bool> RequestCancellationAsync(int importRunId);
        Task<int> RecoverInterruptedAsync();
    }
}
