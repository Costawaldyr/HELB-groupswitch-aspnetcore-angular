using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Models
{
    public record GroupSchedule(string GroupName, IReadOnlyCollection<int> WeekNumbers, IReadOnlyList<ScheduledSession> Sessions);
}