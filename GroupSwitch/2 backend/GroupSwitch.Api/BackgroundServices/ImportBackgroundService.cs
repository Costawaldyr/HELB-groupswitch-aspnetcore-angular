using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Api.BackgroundServices
{
    public class ImportBackgroundService : BackgroundService
    {
        private readonly IImportQueueService _importQueueService;
        private readonly IServiceScopeFactory _scopeFactory;

        public ImportBackgroundService(IImportQueueService importQueueService, IServiceScopeFactory scopeFactory)
        {
            _importQueueService = importQueueService;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var request = await _importQueueService.DequeueAsync(stoppingToken);

                await RunImportAsync(request, stoppingToken);
            }
        }

        private async Task RunImportAsync(ImportRequest request, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var hyperplanningImportService = scope.ServiceProvider.GetRequiredService<IHyperplanningImportService>();

            Func<string, bool> isGroupWanted = name => request.GroupName is null || name == request.GroupName;
            Func<int, bool> isWeekWanted = week => true;

            await hyperplanningImportService.RunAsync(request.ImportRunId, isGroupWanted, isWeekWanted, stoppingToken);
        }
    }
}
