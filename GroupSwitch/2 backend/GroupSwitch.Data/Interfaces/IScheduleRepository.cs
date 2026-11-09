using GroupSwitch.Data.Entities;

namespace GroupSwitch.Data.Interfaces
{
    public interface IScheduleRepository
    {
        Task<int> ReplaceWeekAsync(
            string groupName,
            string academicYear,
            int week,
            IEnumerable<ScheduledSession> sessions,
            int importRunId,
            DateTime retrievedAtUtc,
            CancellationToken cancellationToken = default);

        Task<bool> HasGroupAsync(string groupName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ScheduledSession>> GetSessionsAsync(
            string groupName,
            string academicYear,
            int firstWeek,
            int lastWeek,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ScheduleCoverage>> GetCoveragesAsync(
            string groupName,
            string academicYear,
            int firstWeek,
            int lastWeek,
            CancellationToken cancellationToken = default);
    }
}
