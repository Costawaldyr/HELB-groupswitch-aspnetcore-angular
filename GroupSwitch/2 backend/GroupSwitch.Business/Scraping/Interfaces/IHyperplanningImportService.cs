using GroupSwitch.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IHyperplanningImportService
    {
        Task<ImportRun> StartAsync();
        Task RunAsync(int importRunId, Func<string, bool> isGroupWanted, Func<int, bool> isWeekWanted, CancellationToken cancellationToken = default);
        Task<ImportRun> GetAsync(int importRunId);
        Task<IEnumerable<ImportRun>> GetLatestAsync(int count);
    }
}
