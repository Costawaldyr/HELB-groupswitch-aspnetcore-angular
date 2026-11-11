namespace GroupSwitch.Api.Dtos
{
    public record ImportDetailsDto(
        ImportRunDto Run, 
        IEnumerable<ImportLogDto> Log
    );

}
