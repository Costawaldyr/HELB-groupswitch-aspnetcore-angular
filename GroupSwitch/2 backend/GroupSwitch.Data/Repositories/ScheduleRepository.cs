using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GroupSwitch.Data.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private const string SessionOfAnotherWeekMessage = "A session belongs to another week than the replaced week.";
        private const string SessionWithoutActivityMessage = "A session has no activity.";

        private readonly GroupSwitchDbContext _dbContext;

        public ScheduleRepository(GroupSwitchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> ReplaceWeekAsync(
            string groupName,
            string academicYear,
            int week,
            IEnumerable<ScheduledSession> sessions,
            int importRunId,
            DateTime retrievedAtUtc,
            CancellationToken cancellationToken = default)
        {
            var newSessions = sessions.ToList();

            if (newSessions.Any(session => session.WeekNumber != week))
            {
                throw new ArgumentException(SessionOfAnotherWeekMessage);
            }

            if (newSessions.Any(session => session.Activity is null))
            {
                throw new ArgumentException(SessionWithoutActivityMessage);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var group = await FindOrCreateGroupAsync(groupName, cancellationToken);
            var activities = await LoadActivitiesByTitleAsync(cancellationToken);

            await _dbContext.ScheduledSessions
                .Where(session => session.StudentGroupId == group.Id
                    && session.AcademicYear == academicYear
                    && session.WeekNumber == week)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var session in newSessions)
            {
                session.Activity = ActivityFor(TitleOf(session), activities);
                session.StudentGroup = group;
                session.AcademicYear = academicYear;
            }

            _dbContext.ScheduledSessions.AddRange(newSessions);
            await SaveCoverageAsync(group, academicYear, week, newSessions.Count, importRunId, retrievedAtUtc, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return newSessions.Count;
        }

        public Task<bool> HasGroupAsync(string groupName, CancellationToken cancellationToken = default)
        {
            return _dbContext.StudentGroups.AnyAsync(group => group.Name == groupName, cancellationToken);
        }

        public async Task<IReadOnlyList<ScheduledSession>> GetSessionsAsync(
            string groupName,
            string academicYear,
            int firstWeek,
            int lastWeek,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.StudentGroups
                .Where(group => group.Name == groupName)
                .SelectMany(group => group.SessionList)
                .Where(session => session.AcademicYear == academicYear
                    && session.WeekNumber >= firstWeek
                    && session.WeekNumber <= lastWeek)
                .Include(session => session.Activity)
                .OrderBy(session => session.Start)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ScheduleCoverage>> GetCoveragesAsync(
            string groupName,
            string academicYear,
            int firstWeek,
            int lastWeek,
            CancellationToken cancellationToken = default)
        {
            var groupIds = _dbContext.StudentGroups
                .Where(group => group.Name == groupName)
                .Select(group => group.Id);

            return await _dbContext.ScheduleCoverages
                .Where(coverage => groupIds.Contains(coverage.StudentGroupId)
                    && coverage.AcademicYear == academicYear
                    && coverage.WeekNumber >= firstWeek
                    && coverage.WeekNumber <= lastWeek)
                .OrderBy(coverage => coverage.WeekNumber)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        private async Task SaveCoverageAsync(
            StudentGroup group,
            string academicYear,
            int week,
            int sessionCount,
            int importRunId,
            DateTime retrievedAtUtc,
            CancellationToken cancellationToken)
        {
            var coverage = await _dbContext.ScheduleCoverages
                .FirstOrDefaultAsync(existing => existing.StudentGroupId == group.Id
                    && existing.AcademicYear == academicYear
                    && existing.WeekNumber == week, cancellationToken);

            if (coverage is null)
            {
                coverage = new ScheduleCoverage
                {
                    StudentGroup = group,
                    AcademicYear = academicYear,
                    WeekNumber = week
                };
                _dbContext.ScheduleCoverages.Add(coverage);
            }

            coverage.LastVerifiedAtUtc = retrievedAtUtc;
            coverage.SessionCount = sessionCount;
            coverage.LastImportRunId = importRunId;
        }

        private async Task<StudentGroup> FindOrCreateGroupAsync(string groupName, CancellationToken cancellationToken)
        {
            var existing = await _dbContext.StudentGroups
                .FirstOrDefaultAsync(group => group.Name == groupName, cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            var created = new StudentGroup { Name = groupName };
            _dbContext.StudentGroups.Add(created);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return created;
        }

        private async Task<Dictionary<string, Activity>> LoadActivitiesByTitleAsync(CancellationToken cancellationToken)
        {
            var activities = await _dbContext.Activities.ToListAsync(cancellationToken);

            return activities.ToDictionary(activity => activity.Title, StringComparer.OrdinalIgnoreCase);
        }

        private string TitleOf(ScheduledSession session)
        {
            if (session.Activity is null)
            {
                throw new ArgumentException(SessionWithoutActivityMessage);
            }

            return session.Activity.Title;
        }

        private Activity ActivityFor(string title, Dictionary<string, Activity> activities)
        {
            if (!activities.TryGetValue(title, out var activity))
            {
                activity = new Activity { Title = title };
                activities[title] = activity;
            }

            return activity;
        }
    }
}
