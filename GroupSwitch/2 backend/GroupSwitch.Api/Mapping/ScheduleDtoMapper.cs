using GroupSwitch.Api.Dtos;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Api.Mapping
{
    public class ScheduleDtoMapper
    {
        private const string ActivityNotLoadedMessage = "The activity of a session was not loaded.";

        public ScheduleDto ToDto(GroupScheduleOverview overview)
        {
            var weeks = overview.Weeks.Select(ToDto).ToList();

            return new ScheduleDto(overview.GroupName, overview.AcademicYear, weeks);
        }

        private WeekScheduleDto ToDto(WeekSchedule week)
        {
            var sessions = week.Sessions.Select(ToDto).ToList();

            return new WeekScheduleDto(week.Week, week.Coverage.ToString(), week.LastVerifiedAtUtc, sessions);
        }

        private SessionDto ToDto(ScheduledSession session)
        {
            if (session.Activity is null)
            {
                throw new InvalidOperationException(ActivityNotLoadedMessage);
            }

            return new SessionDto(session.Activity.Title, session.Start, session.End, session.Room, session.Teacher, session.Remark, session.IsCancelled);
        }
    }
}
