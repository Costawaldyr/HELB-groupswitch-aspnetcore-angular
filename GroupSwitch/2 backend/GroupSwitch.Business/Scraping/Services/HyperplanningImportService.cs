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
        private readonly IHyperplanningScraper _scraper;
        private readonly IWeekAssessmentService _weekAssessmentService;
        private readonly IGroupScheduleCollectorService _groupScheduleCollectorService;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IImportRunRepository _importRunRepository;
        private readonly IImportLogService _importLogService;
        private readonly HyperplanningSettings _settings;

        public HyperplanningImportService(
            IHyperplanningScraper scraper,
            IWeekAssessmentService weekAssessmentService,
            IGroupScheduleCollectorService groupScheduleCollectorService,
            IScheduleRepository scheduleRepository,
            IImportRunRepository importRunRepository,
            IImportLogService importLogService,
            IOptions<HyperplanningSettings> settings)
        {
            _scraper = scraper;
            _weekAssessmentService = weekAssessmentService;
            _groupScheduleCollectorService = groupScheduleCollectorService;
            _scheduleRepository = scheduleRepository;
            _importRunRepository = importRunRepository;
            _importLogService = importLogService;
            _settings = settings.Value;
        }

        public async Task<ImportRun> StartAsync()
        {
            if (await _importRunRepository.HasRunningImportAsync())
            {
                throw new ImportAlreadyRunningException();
            }

            return await _importRunRepository.StartAsync();
        }

        public Task<ImportRun> GetAsync(int importRunId)
        {
            return _importRunRepository.GetByIdAsync(importRunId);
        }

        public Task<IEnumerable<ImportRun>> GetLatestAsync(int count)
        {
            return _importRunRepository.GetLatestAsync(count);
        }

        public async Task RunAsync(int importRunId, Func<string, bool> isGroupWanted, Func<int, bool> isWeekWanted, CancellationToken cancellationToken = default)
        {
            var totals = new ImportTotals();

            await _importLogService.InfoAsync(importRunId, ImportMessages.Started);

            try
            {
                var allGroupNames = await _scraper.GetGroupNamesAsync(cancellationToken);
                var groupNames = allGroupNames.Where(isGroupWanted).ToList();
                var weeks = WeeksToVisit(isWeekWanted);

                var retrievals = _scraper.ScrapeAsync(groupNames, weeks, cancellationToken);
                var assessments = AssessAsync(importRunId, retrievals);

                await foreach (var groupSchedule in _groupScheduleCollectorService.CollectAsync(assessments))
                {
                    await SaveGroupAsync(importRunId, groupSchedule, totals);
                }

                await CompleteAsync(importRunId, totals);
            }
            catch (Exception exception)
            {
                await FailAsync(importRunId, exception, totals);
            }
        }

        private List<int> WeeksToVisit(Func<int, bool> isWeekWanted)
        {
            var weekCount = _settings.LastWeek - _settings.FirstWeek + 1;

            return Enumerable.Range(_settings.FirstWeek, weekCount).Where(isWeekWanted).ToList();
        }

        private async IAsyncEnumerable<WeekAssessment> AssessAsync(int importRunId, IAsyncEnumerable<WeekRetrieval> retrievals)
        {
            await foreach (var retrieval in retrievals)
            {
                var assessment = _weekAssessmentService.Assess(retrieval);

                if (!assessment.IsAccepted)
                {
                    var message = string.Format(ImportMessages.WeekRejectedFormat, assessment.GroupName, assessment.Week, assessment.Status, assessment.Diagnostic);
                    await _importLogService.ErrorAsync(importRunId, message);
                }

                yield return assessment;
            }
        }

        private async Task SaveGroupAsync(int importRunId, GroupSchedule groupSchedule, ImportTotals totals)
        {
            var count = await _scheduleRepository.ReplaceGroupWeeksAsync(groupSchedule.GroupName, groupSchedule.WeekNumbers, groupSchedule.Sessions);

            totals.AddGroup(count);
            await _importLogService.InfoAsync(importRunId,
                string.Format(ImportMessages.GroupSavedFormat, groupSchedule.GroupName, count, groupSchedule.WeekNumbers.Count));
        }

        private async Task CompleteAsync(int importRunId, ImportTotals totals)
        {
            await _importLogService.InfoAsync(importRunId, string.Format(ImportMessages.FinishedFormat, totals.Groups, totals.Sessions));
            await _importRunRepository.FinishAsync(importRunId, ImportStatus.Finished, totals.Groups, totals.Sessions);
        }

        private async Task FailAsync(int importRunId, Exception exception, ImportTotals totals)
        {
            await _importLogService.ErrorAsync(importRunId, exception.Message);
            await _importRunRepository.FinishAsync(importRunId, ImportStatus.Failed, totals.Groups, totals.Sessions);
        }
    }
}
