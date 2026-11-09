using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IImportQueueService
    {
        void Enqueue(QueuedImport queuedImport);
        ValueTask<QueuedImport> DequeueAsync(CancellationToken cancellationToken);
    }
}
