using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Interfaces;
using Microsoft.Extensions.Options;

namespace GroupSwitch.Business.Scraping.Services
{
    public class HyperplanningImportService : IHyperplanningImportService
    {
        private static readonly SemaphoreSlim StartLock = new(1, 1);

        private readonly IHyperplanningScraper _scraper;
        private readonly IWeekAssessmentService _weekAssessmentService;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IImportRunRepository _importRunRepository;
        private readonly IImportLogService _importLogService;
        private readonly HyperplanningSettings _settings;

        public HyperplanningImportService(
            IHyperplanningScraper scraper,
            IWeekAssessmentService weekAssessmentService,
            IScheduleRepository scheduleRepository,
            IImportRunRepository importRunRepository,
            IImportLogService importLogService,
            IOptions<HyperplanningSettings> settings)
        {
            _scraper = scraper;
            _weekAssessmentService = weekAssessmentService;
            _scheduleRepository = scheduleRepository;
            _importRunRepository = importRunRepository;
            _importLogService = importLogService;
            _settings = settings.Value;
        }

        public async Task<ImportRun> StartAsync(ImportRequest request, CancellationToken cancellationToken = default)
        {
            var weekRange = _settings.ResolveWeekRange(request.FirstWeek, request.LastWeek);

            if (request.GroupName is not null)
            {
                await EnsureGroupExistsAsync(request.GroupName, cancellationToken);
            }

            await StartLock.WaitAsync(cancellationToken);

            try
            {
                if (await _importRunRepository.HasRunningImportAsync())
                {
                    throw new ImportAlreadyRunningException();
                }

                return await _importRunRepository.StartAsync(request.GroupName, _settings.AcademicYear, weekRange.FirstWeek, weekRange.LastWeek);
            }
            finally
            {
                StartLock.Release();
            }
        }

        public Task<ImportRun> GetAsync(int importRunId)
        {
            return _importRunRepository.GetByIdAsync(importRunId);
        }

        public Task<IEnumerable<ImportRun>> GetLatestAsync(int count)
        {
            return _importRunRepository.GetLatestAsync(count);
        }

        public Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken cancellationToken = default)
        {
            return _scraper.GetGroupNamesAsync(cancellationToken);
        }

        public Task<bool> RequestCancellationAsync(int importRunId)
        {
            return _importRunRepository.RequestCancellationAsync(importRunId);
        }

        public Task<int> RecoverInterruptedAsync()
        {
            return _importRunRepository.RecoverInterruptedAsync(ImportMessages.Interrupted, ImportLogLevels.Error);
        }

        public async Task RunAsync(int importRunId, CancellationToken cancellationToken = default)
        {
            var run = await _importRunRepository.GetByIdAsync(importRunId);
            var totals = new ImportTotals();

            await _importLogService.InfoAsync(importRunId, ImportMessages.Started);

            try
            {
                var status = await ImportWeeksAsync(run, totals, cancellationToken);
                await FinishAsync(importRunId, status, totals);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await _importLogService.ErrorAsync(importRunId, ImportMessages.Shutdown);
                await FinishAsync(importRunId, ImportStatus.Interrupted, totals);
            }
            catch (Exception exception)
            {
                await _importLogService.ErrorAsync(importRunId, exception.Message);
                await FinishAsync(importRunId, ImportStatus.Failed, totals);
            }
        }

        private async Task<ImportStatus> ImportWeeksAsync(ImportRun run, ImportTotals totals, CancellationToken cancellationToken)
        {
            if (run.AcademicYear is null || run.FirstWeek is null || run.LastWeek is null)
            {
                throw new InvalidOperationException(ImportMessages.MissingScope);
            }

            var weeks = new WeekRange(run.FirstWeek.Value, run.LastWeek.Value).Weeks().ToList();
            var groupNames = await GroupNamesToImportAsync(run.RequestedGroupName, cancellationToken);

            await foreach (var retrieval in _scraper.ScrapeAsync(groupNames, weeks, cancellationToken))
            {
                await ImportWeekAsync(run.Id, run.AcademicYear, retrieval, totals, cancellationToken);

                if (await _importRunRepository.IsCancellationRequestedAsync(run.Id))
                {
                    await _importLogService.InfoAsync(run.Id, ImportMessages.Cancelled);

                    return ImportStatus.Cancelled;
                }
            }

            return FinalStatusOf(totals);
        }

        private async Task ImportWeekAsync(int importRunId, string academicYear, WeekRetrieval retrieval, ImportTotals totals, CancellationToken cancellationToken)
        {
            var assessment = _weekAssessmentService.Assess(retrieval);

            if (assessment.IsAccepted)
            {
                var sessionCount = await _scheduleRepository.ReplaceWeekAsync(
                    assessment.GroupName,
                    academicYear,
                    assessment.Week,
                    assessment.Sessions,
                    importRunId,
                    retrieval.RetrievedAtUtc,
                    cancellationToken);

                totals.AddSavedWeek(assessment.GroupName, sessionCount);
            }
            else
            {
                var message = string.Format(ImportMessages.WeekRejectedFormat, assessment.GroupName, assessment.Week, assessment.Status, assessment.Diagnostic);
                await _importLogService.ErrorAsync(importRunId, message);
                totals.AddRejectedWeek();
            }

            await _importRunRepository.UpdateWeekCountsAsync(importRunId, totals.SuccessfulWeeks, totals.RejectedWeeks);
        }

        private ImportStatus FinalStatusOf(ImportTotals totals)
        {
            if (totals.SuccessfulWeeks == 0)
            {
                return ImportStatus.Failed;
            }

            return totals.RejectedWeeks == 0 ? ImportStatus.Finished : ImportStatus.PartiallyCompleted;
        }

        private async Task<IReadOnlyCollection<string>> GroupNamesToImportAsync(string? requestedGroupName, CancellationToken cancellationToken)
        {
            if (requestedGroupName is not null)
            {
                return new[] { requestedGroupName };
            }

            return await _scraper.GetGroupNamesAsync(cancellationToken);
        }

        private async Task EnsureGroupExistsAsync(string groupName, CancellationToken cancellationToken)
        {
            var groupNames = await _scraper.GetGroupNamesAsync(cancellationToken);

            if (!groupNames.Contains(groupName))
            {
                throw new UnknownGroupException(groupName);
            }
        }

        private async Task FinishAsync(int importRunId, ImportStatus status, ImportTotals totals)
        {
            foreach (var summary in totals.GroupSummaries)
            {
                await _importLogService.InfoAsync(importRunId,
                    string.Format(ImportMessages.GroupSavedFormat, summary.GroupName, summary.SessionCount, summary.WeekCount));
            }

            await _importLogService.InfoAsync(importRunId,
                string.Format(ImportMessages.FinishedFormat, status, totals.Groups, totals.Sessions, totals.SuccessfulWeeks, totals.RejectedWeeks));
            await _importRunRepository.UpdateWeekCountsAsync(importRunId, totals.SuccessfulWeeks, totals.RejectedWeeks);
            await _importRunRepository.FinishAsync(importRunId, status, totals.Groups, totals.Sessions);
        }
    }
}
