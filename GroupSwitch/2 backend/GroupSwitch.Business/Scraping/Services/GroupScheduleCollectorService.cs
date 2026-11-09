using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Services
{
    public class GroupScheduleCollectorService : IGroupScheduleCollectorService
    {
        private readonly IWeekResponseConverterService _weekResponseConverterService;

        public GroupScheduleCollectorService(IWeekResponseConverterService weekResponseConverterService)
        {
            _weekResponseConverterService = weekResponseConverterService;
        }

        public async IAsyncEnumerable<GroupSchedule> CollectAsync(IAsyncEnumerable<WeekRetrieval> retrievals, Func<int, bool> isWeekWanted)
        {
            string? currentGroup = null;
            var currentWeeks = new List<int>();
            var currentSessions = new List<ScheduledSession>();

            await foreach (var retrieval in retrievals)
            {
                if (!retrieval.IsSuccessful)
                {
                    continue;
                }

                if (currentGroup is not null && retrieval.GroupName != currentGroup)
                {
                    yield return new GroupSchedule(currentGroup, currentWeeks, currentSessions);
                    currentWeeks = new List<int>();
                    currentSessions = new List<ScheduledSession>();
                }

                currentGroup = retrieval.GroupName;
                currentWeeks.Add(retrieval.RequestedWeek);
                currentSessions.AddRange(_weekResponseConverterService.Convert(retrieval, isWeekWanted));
            }

            if (currentGroup is not null)
            {
                yield return new GroupSchedule(currentGroup, currentWeeks, currentSessions);
            }
        }
    }
}
