using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IWeekResponseConverterService
    {
        IEnumerable<ScheduledSession> Convert(WeekRetrieval retrieval, Func<int, bool> isWeekWanted);
    }
}
