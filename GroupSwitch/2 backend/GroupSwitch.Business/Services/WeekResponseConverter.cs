using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Services
{
    public class WeekResponseConverter : IWeekResponseConverter
    {
        private readonly IWeekScheduleParser _parser;
        private readonly ISessionBuilder _sessionBuilder;

        public WeekResponseConverter(IWeekScheduleParser parser, ISessionBuilder sessionBuilder)
        {
            _parser = parser;
            _sessionBuilder = sessionBuilder;
        }

        public IEnumerable<ScheduledSession> Convert(WeekResponse response, Func<int, bool> isWeekWanted)
        {
            var parsedSessions = _parser.Parse(response.Json);

            return _sessionBuilder.Build(parsedSessions, isWeekWanted);
        }
    }
}