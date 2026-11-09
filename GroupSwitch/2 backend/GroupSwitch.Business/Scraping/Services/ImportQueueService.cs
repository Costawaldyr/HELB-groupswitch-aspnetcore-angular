using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using System.Threading.Channels;

namespace GroupSwitch.Business.Scraping.Services
{
    public class ImportQueueService : IImportQueueService
    {
        private readonly Channel<ImportRequest> channel = Channel.CreateUnbounded<ImportRequest>();

        public void Enqueue(ImportRequest request)
        {
            channel.Writer.TryWrite(request);
        }

        public ValueTask<ImportRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            return channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
