using GroupSwitch.Business.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Interfaces
{
    public interface IWeekResponseConverter
    {
        IEnumerable<ScheduledSession> Convert(WeekResponse response, Func<int, bool> isWeekWanted);
    }
}