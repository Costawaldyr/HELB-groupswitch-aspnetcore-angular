using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using System.Threading.Channels;

namespace GroupSwitch.Business.Scraping.Services
{
    public class ImportQueueService : IImportQueueService
    {
        private readonly Channel<QueuedImport> _channel = Channel.CreateUnbounded<QueuedImport>();

        public void Enqueue(QueuedImport queuedImport)
        {
            _channel.Writer.TryWrite(queuedImport);
        }

        public ValueTask<QueuedImport> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
