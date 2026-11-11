using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IWeekScheduleParser
    {
        IEnumerable<ParsedSession> Parse(string json);
    }
}
