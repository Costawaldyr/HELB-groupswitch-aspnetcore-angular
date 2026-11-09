namespace GroupSwitch.Api.Dtos
{
    public record SessionDto(
        string Activity,
        DateTime Start,
        DateTime End,
        string Room,
        string Teacher,
        string Remark,
        bool IsCancelled
    );
}
