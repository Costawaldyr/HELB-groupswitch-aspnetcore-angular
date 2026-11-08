using GroupSwitch.Business.Models;

namespace GroupSwitch.Business.Interfaces
{
    public interface IGroupScheduleCollector
    {
        IAsyncEnumerable<GroupSchedule> CollectAsync(IAsyncEnumerable<WeekResponse> responses, Func<int, bool> isWeekWanted);
    }
}