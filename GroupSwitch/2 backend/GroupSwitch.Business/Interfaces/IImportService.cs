using GroupSwitch.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Interfaces
{
    public interface IImportService
    {
        Task<ImportRun> StartAsync();
        Task RunAsync(int importRunId, Func<string, bool> isGroupWanted, Func<int, bool> isWeekWanted);
        Task<ImportRun> GetAsync(int importRunId);
        Task<IEnumerable<ImportRun>> GetLatestAsync(int count);
    }
}
