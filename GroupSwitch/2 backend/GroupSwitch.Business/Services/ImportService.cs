using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Interfaces;
using GroupSwitch.Data.Repositories;

namespace GroupSwitch.Business.Services
{
    public class ImportService : IImportService
    {
        private readonly IHyperplanningScraper _scraper;
        private readonly IGroupScheduleCollector _collector;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IImportRunRepository _importRunRepository;
        private readonly IImportJournal _journal;

        public ImportService(IHyperplanningScraper scraper, IGroupScheduleCollector collector, IScheduleRepository scheduleRepository, IImportRunRepository importRunRepository, IImportJournal journal)
        {
            _scraper = scraper;
            _collector = collector;
            _scheduleRepository = scheduleRepository;
            _importRunRepository = importRunRepository;
            _journal = journal;
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

        public async Task RunAsync(int importRunId, Func<string, bool> isGroupWanted, Func<int, bool> isWeekWanted)
        {
            var totals = new ImportTotals();

            await _journal.InfoAsync(importRunId, ImportMessages.Started);

            try
            {
                var responses = _scraper.ScrapeAsync(isGroupWanted, isWeekWanted);

                await foreach (var groupSchedule in _collector.CollectAsync(responses, isWeekWanted))
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

        private async Task SaveGroupAsync(int importRunId, GroupSchedule groupSchedule, ImportTotals totals)
        {
            var count = await _scheduleRepository.ReplaceGroupWeeksAsync(groupSchedule.GroupName, groupSchedule.WeekNumbers, groupSchedule.Sessions);

            totals.AddGroup(count);
            await _journal.InfoAsync(importRunId,
                string.Format(ImportMessages.GroupSavedFormat, groupSchedule.GroupName, count, groupSchedule.WeekNumbers.Count));
        }

        private async Task CompleteAsync(int importRunId, ImportTotals totals)
        {
            await _journal.InfoAsync(importRunId, string.Format(ImportMessages.FinishedFormat, totals.Groups, totals.Sessions));
            await _importRunRepository.FinishAsync(importRunId, ImportStatus.Finished, totals.Groups, totals.Sessions);
        }

        private async Task FailAsync(int importRunId, Exception exception, ImportTotals totals)
        {
            await _journal.ErrorAsync(importRunId, exception.Message);
            await _importRunRepository.FinishAsync(importRunId, ImportStatus.Failed, totals.Groups, totals.Sessions);
        }
    }
}