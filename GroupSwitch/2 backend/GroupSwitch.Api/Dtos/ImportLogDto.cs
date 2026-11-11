namespace GroupSwitch.Api.Dtos
{
    public record ImportLogDto(
        DateTime Moment, 
        string Level, 
        string Message
    );
}
