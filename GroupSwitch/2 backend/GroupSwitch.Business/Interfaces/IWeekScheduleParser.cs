using GroupSwitch.Business.Models;

namespace GroupSwitch.Business.Interfaces
{
    public interface IWeekScheduleParser
    {
        IEnumerable<ParsedSession> Parse(string json);
    }
}
