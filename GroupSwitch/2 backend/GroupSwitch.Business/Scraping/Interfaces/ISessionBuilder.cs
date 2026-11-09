using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface ISessionBuilder
    {
        IEnumerable<ScheduledSession> Build(IEnumerable<ParsedSession> parsedSessions, Func<int, bool> isWeekWanted);
    }
}