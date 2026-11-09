using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Interfaces;
using Microsoft.Extensions.Options;

namespace GroupSwitch.Business.Scraping.Services
{
    public class ScheduleQueryService : IScheduleQueryService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly HyperplanningSettings _settings;

        public ScheduleQueryService(IScheduleRepository scheduleRepository, IOptions<HyperplanningSettings> settings)
        {
            _scheduleRepository = scheduleRepository;
            _settings = settings.Value;
        }

        public async Task<GroupScheduleOverview> GetScheduleAsync(string groupName, int? firstWeek, int? lastWeek, CancellationToken cancellationToken = default)
        {
            var weekRange = _settings.ResolveWeekRange(firstWeek, lastWeek);

            if (!await _scheduleRepository.HasGroupAsync(groupName, cancellationToken))
            {
                throw new UnknownGroupException(groupName);
            }

            var sessions = await _scheduleRepository.GetSessionsAsync(groupName, _settings.AcademicYear, weekRange.FirstWeek, weekRange.LastWeek, cancellationToken);
            var coverages = await _scheduleRepository.GetCoveragesAsync(groupName, _settings.AcademicYear, weekRange.FirstWeek, weekRange.LastWeek, cancellationToken);

            var weeks = weekRange.Weeks()
                .Select(week => WeekScheduleOf(week, sessions, coverages))
                .ToList();

            return new GroupScheduleOverview(groupName, _settings.AcademicYear, weeks);
        }

        private WeekSchedule WeekScheduleOf(int week, IReadOnlyList<ScheduledSession> sessions, IReadOnlyList<ScheduleCoverage> coverages)
        {
            var weekSessions = sessions.Where(session => session.WeekNumber == week).ToList();
            var coverage = coverages.FirstOrDefault(existing => existing.WeekNumber == week);

            if (coverage is null)
            {
                return new WeekSchedule(week, WeekCoverageStatus.Unknown, null, weekSessions);
            }

            var status = coverage.SessionCount == 0 ? WeekCoverageStatus.VerifiedEmpty : WeekCoverageStatus.Verified;

            return new WeekSchedule(week, status, coverage.LastVerifiedAtUtc, weekSessions);
        }
    }
}
