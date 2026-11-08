using GroupSwitch.Business.Models;

namespace GroupSwitch.Business.Interfaces
{
    public interface IImportQueue
    {
        void Enqueue(ImportRequest request);
        ValueTask<ImportRequest> DequeueAsync(CancellationToken cancellationToken);
    }
}
