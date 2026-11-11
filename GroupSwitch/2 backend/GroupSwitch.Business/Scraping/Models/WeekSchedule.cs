using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Models
{
    public record WeekSchedule(
        int Week,
        WeekCoverageStatus Coverage,
        DateTime? LastVerifiedAtUtc,
        IReadOnlyList<ScheduledSession> Sessions);
}
