using GroupSwitch.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupSwitch.Data.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private const string SessionOutsideScrapedWeeksMessage = "A session belongs to a week that was not scraped.";
        private const string SessionWithoutActivityMessage = "A session has no activity.";

        private readonly GroupSwitchDbContext _dbContext;

        public ScheduleRepository(GroupSwitchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> ReplaceGroupWeeksAsync(string groupName, IReadOnlyCollection<int> scrapedWeeks, IEnumerable<ScheduledSession> sessions)
        {
            var weeks = scrapedWeeks.Distinct().ToList();
            var newSessions = sessions.ToList();

            if (newSessions.Any(session => !weeks.Contains(session.WeekNumber)))
            {
                throw new ArgumentException(SessionOutsideScrapedWeeksMessage);
            }

            if (!weeks.Any())
            {
                return 0;
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var group = await FindOrCreateGroupAsync(groupName);
            var activities = await LoadActivitiesByTitleAsync();

            await _dbContext.ScheduledSessions
                .Where(session => session.StudentGroupId == group.Id && weeks.Contains(session.WeekNumber))
                .ExecuteDeleteAsync();

            foreach (var session in newSessions)
            {
                session.Activity = ActivityFor(TitleOf(session), activities);
                session.StudentGroup = group;
            }

            _dbContext.ScheduledSessions.AddRange(newSessions);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return newSessions.Count;
        }

        public async Task<IEnumerable<ScheduledSession>> GetSessionsOfGroupAsync(string groupName)
        {
            return await _dbContext.StudentGroups
                .Where(group => group.Name == groupName)
                .SelectMany(group => group.SessionList)
                .Include(session => session.Activity)
                .OrderBy(session => session.Start)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<StudentGroup> FindOrCreateGroupAsync(string groupName)
        {
            var existing = await _dbContext.StudentGroups
                .Where(group => group.Name == groupName)
                .ToListAsync();

            if (existing.Any())
            {
                return existing.First();
            }

            var created = new StudentGroup { Name = groupName };
            _dbContext.StudentGroups.Add(created);
            await _dbContext.SaveChangesAsync();

            return created;
        }

        private async Task<Dictionary<string, Activity>> LoadActivitiesByTitleAsync()
        {
            var activities = await _dbContext.Activities.ToListAsync();

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