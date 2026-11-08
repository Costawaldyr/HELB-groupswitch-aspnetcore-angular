using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;

namespace GroupSwitch.Api.BackgroundServices
{
    public class ImportBackgroundService : BackgroundService
    {
        private readonly IImportQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public ImportBackgroundService(IImportQueue queue, IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var request = await _queue.DequeueAsync(stoppingToken);

                await RunImportAsync(request);
            }
        }

        private async Task RunImportAsync(ImportRequest request)
        {
            using var scope = _scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            Func<string, bool> isGroupWanted = name => request.GroupName is null || name == request.GroupName;
            await importService.RunAsync(request.ImportRunId, isGroupWanted, QuadrimesterFilters.IsInFirstQuadrimester);
        }
    }
}