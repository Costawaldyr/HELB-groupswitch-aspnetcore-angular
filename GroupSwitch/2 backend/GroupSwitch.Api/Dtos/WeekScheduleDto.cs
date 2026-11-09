namespace GroupSwitch.Api.Dtos
{
    public record WeekScheduleDto(
        int Week,
        string Coverage,
        DateTime? LastVerifiedAtUtc,
        IEnumerable<SessionDto> Sessions
    );
}
