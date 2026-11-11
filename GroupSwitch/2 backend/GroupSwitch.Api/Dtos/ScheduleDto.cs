namespace GroupSwitch.Api.Dtos
{
    public record ScheduleDto(
        string GroupName,
        string AcademicYear,
        IEnumerable<WeekScheduleDto> Weeks
    );
}
