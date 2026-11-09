namespace GroupSwitch.Business.Scraping.Models
{
    public record GroupScheduleOverview(
        string GroupName,
        string AcademicYear,
        IReadOnlyList<WeekSchedule> Weeks);
}
