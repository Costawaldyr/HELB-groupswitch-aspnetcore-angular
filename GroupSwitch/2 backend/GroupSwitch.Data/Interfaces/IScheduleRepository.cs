using GroupSwitch.Data.Entities;

namespace GroupSwitch.Data.Interfaces
{
    public interface IScheduleRepository
    {
        /// <summary>
        /// Replaces only the sessions of the weeks that were actually scraped for this group.
        /// Sessions of other weeks are never touched, so a partial import cannot lose data.
        /// </summary>
        Task<int> ReplaceGroupWeeksAsync(string groupName, IReadOnlyCollection<int> scrapedWeeks, IEnumerable<ScheduledSession> sessions);

        Task<IEnumerable<ScheduledSession>> GetSessionsOfGroupAsync(string groupName);
    }
}