using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Services
{
    public class GroupScheduleCollectorService : IGroupScheduleCollectorService
    {
        public async IAsyncEnumerable<GroupSchedule> CollectAsync(IAsyncEnumerable<WeekAssessment> assessments)
        {
            string? currentGroup = null;
            var currentWeeks = new List<int>();
            var currentSessions = new List<ScheduledSession>();

            await foreach (var assessment in assessments)
            {
                if (!assessment.IsAccepted)
                {
                    continue;
                }

                if (currentGroup is not null && assessment.GroupName != currentGroup)
                {
                    yield return new GroupSchedule(currentGroup, currentWeeks, currentSessions);
                    currentWeeks = new List<int>();
                    currentSessions = new List<ScheduledSession>();
                }

                currentGroup = assessment.GroupName;
                currentWeeks.Add(assessment.Week);
                currentSessions.AddRange(assessment.Sessions);
            }

            if (currentGroup is not null)
            {
                yield return new GroupSchedule(currentGroup, currentWeeks, currentSessions);
            }
        }
    }
}
