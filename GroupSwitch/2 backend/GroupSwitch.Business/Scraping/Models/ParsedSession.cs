namespace GroupSwitch.Business.Scraping.Models
{
    public record ParsedSession(
        int Week,
        int Position,
        int DurationInSlots,
        string Title,
        string Teacher,
        string Room,
        string Remark,
        bool IsCancelled
    );
}