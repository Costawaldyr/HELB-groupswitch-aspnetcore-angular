using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Data.Interfaces;
using GroupSwitch.Data.Repositories;

namespace GroupSwitch.Business.Services
{
    public class ImportJournal : IImportJournal
    {
        private readonly IImportRunRepository _importRunRepository;

        public ImportJournal(IImportRunRepository importRunRepository)
        {
            _importRunRepository = importRunRepository;
        }

        public Task InfoAsync(int importRunId, string message)
        {
            return _importRunRepository.AddLogAsync(importRunId, ImportLogLevels.Info, message);
        }

        public Task ErrorAsync(int importRunId, string message)
        {
            return _importRunRepository.AddLogAsync(importRunId, ImportLogLevels.Error, message);
        }
    }
}