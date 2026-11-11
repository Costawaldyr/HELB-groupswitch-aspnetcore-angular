using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IScheduleQueryService
    {
        Task<GroupScheduleOverview> GetScheduleAsync(string groupName, int? firstWeek, int? lastWeek, CancellationToken cancellationToken = default);
    }
}
