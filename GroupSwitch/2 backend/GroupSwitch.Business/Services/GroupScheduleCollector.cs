using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Data.Entities;
using System;

namespace GroupSwitch.Business.Services
{
    public class GroupScheduleCollector : IGroupScheduleCollector
    {
        private readonly IWeekResponseConverter _converter;
        public GroupScheduleCollector(IWeekResponseConverter converter)
        {
            _converter = converter;
        }

        public async IAsyncEnumerable<GroupSchedule> CollectAsync(IAsyncEnumerable<WeekResponse> responses, Func<int, bool> isWeekWanted)
        {
            string? currentGroup = null;
            var currentWeeks = new List<int>();
            var currentSessions = new List<ScheduledSession>();

            await foreach (var response in responses)
            {
                if (currentGroup is not null && response.GroupName != currentGroup)
                {
                    yield return new GroupSchedule(currentGroup, currentWeeks, currentSessions);
                    currentWeeks = new List<int>();
                    currentSessions = new List<ScheduledSession>();
                }

                currentGroup = response.GroupName;
                currentWeeks.Add(response.Week);
                currentSessions.AddRange(_converter.Convert(response, isWeekWanted));
            }

            if (currentGroup is not null)
            {
                yield return new GroupSchedule(currentGroup, currentWeeks, currentSessions);
            }
        }
    }
}