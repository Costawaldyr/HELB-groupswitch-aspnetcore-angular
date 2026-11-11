using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Models
{
    public record WeekAssessment(
        string GroupName,
        int Week,
        WeekResponseStatus Status,
        IReadOnlyList<ScheduledSession> Sessions,
        string Diagnostic)
    {
        public bool IsAccepted => Status is WeekResponseStatus.Complete or WeekResponseStatus.CompleteEmpty;
    }
}
