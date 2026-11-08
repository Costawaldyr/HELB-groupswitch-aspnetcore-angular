using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Models
{
    public record GroupSchedule(string GroupName, IReadOnlyCollection<int> WeekNumbers, IReadOnlyList<ScheduledSession> Sessions);
}