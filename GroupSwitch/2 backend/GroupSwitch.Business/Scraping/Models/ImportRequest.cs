namespace GroupSwitch.Business.Scraping.Models
{
    public record ImportRequest(string? GroupName, int? FirstWeek, int? LastWeek);
}
