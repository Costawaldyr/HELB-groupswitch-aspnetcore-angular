using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IImportQueueService
    {
        void Enqueue(ImportRequest request);
        ValueTask<ImportRequest> DequeueAsync(CancellationToken cancellationToken);
    }
}
