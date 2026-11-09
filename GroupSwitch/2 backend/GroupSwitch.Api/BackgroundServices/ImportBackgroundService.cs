using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Api.BackgroundServices
{
    public class ImportBackgroundService : BackgroundService
    {
        private const string RecoveryFailedMessage = "Impossible de marquer les imports interrompus.";
        private const string RecoveredFormat = "{0} import(s) marqué(s) Interrupted au démarrage.";
        private const string ImportFailedFormat = "L'import {0} s'est arrêté sur une erreur inattendue.";
        private const string DequeueFailedMessage = "Lecture de la file d'imports impossible.";

        private readonly IImportQueueService _importQueueService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ImportBackgroundService> _logger;

        public ImportBackgroundService(IImportQueueService importQueueService, IServiceScopeFactory scopeFactory, ILogger<ImportBackgroundService> logger)
        {
            _importQueueService = importQueueService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RecoverInterruptedImportsAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                QueuedImport queuedImport;

                try
                {
                    queuedImport = await _importQueueService.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, DequeueFailedMessage);
                    continue;
                }

                await RunImportAsync(queuedImport, stoppingToken);
            }
        }

        private async Task RecoverInterruptedImportsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var hyperplanningImportService = scope.ServiceProvider.GetRequiredService<IHyperplanningImportService>();

                var recoveredCount = await hyperplanningImportService.RecoverInterruptedAsync();
                _logger.LogInformation(RecoveredFormat, recoveredCount);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, RecoveryFailedMessage);
            }
        }

        private async Task RunImportAsync(QueuedImport queuedImport, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var hyperplanningImportService = scope.ServiceProvider.GetRequiredService<IHyperplanningImportService>();

                await hyperplanningImportService.RunAsync(queuedImport.ImportRunId, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, ImportFailedFormat, queuedImport.ImportRunId);
            }
        }
    }
}
