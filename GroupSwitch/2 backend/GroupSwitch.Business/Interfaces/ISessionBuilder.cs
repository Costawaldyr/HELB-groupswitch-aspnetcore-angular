using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Interfaces
{
    public interface ISessionBuilder
    {
        IEnumerable<ScheduledSession> Build(IEnumerable<ParsedSession> parsedSessions, Func<int, bool> isWeekWanted);
    }
}