using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using System.Threading.Channels;

namespace GroupSwitch.Business.Services
{
    public class ImportQueue : IImportQueue
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
