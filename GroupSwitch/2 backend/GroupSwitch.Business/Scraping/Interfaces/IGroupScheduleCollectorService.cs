using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IGroupScheduleCollectorService
    {
        IAsyncEnumerable<GroupSchedule> CollectAsync(IAsyncEnumerable<WeekAssessment> assessments);
    }
}
