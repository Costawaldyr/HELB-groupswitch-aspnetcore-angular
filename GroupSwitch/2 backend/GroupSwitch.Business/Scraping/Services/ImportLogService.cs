using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Interfaces;

namespace GroupSwitch.Business.Scraping.Services
{
    public class ImportLogService : IImportLogService
    {
        private readonly IImportRunRepository _importRunRepository;

        public ImportLogService(IImportRunRepository importRunRepository)
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