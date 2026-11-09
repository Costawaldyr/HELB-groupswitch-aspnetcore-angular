using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Services
{
    public class WeekResponseConverterService : IWeekResponseConverterService
    {
        private const string FailedRetrievalMessage = "A failed retrieval has no JSON to convert.";

        private readonly IWeekScheduleParser _parser;
        private readonly ISessionBuilder _sessionBuilder;

        public WeekResponseConverterService(IWeekScheduleParser parser, ISessionBuilder sessionBuilder)
        {
            _parser = parser;
            _sessionBuilder = sessionBuilder;
        }

        public IEnumerable<ScheduledSession> Convert(WeekRetrieval retrieval, Func<int, bool> isWeekWanted)
        {
            if (!retrieval.IsSuccessful)
            {
                throw new ArgumentException(FailedRetrievalMessage);
            }

            var parsedSessions = _parser.Parse(retrieval.Json);

            return _sessionBuilder.Build(parsedSessions, isWeekWanted);
        }
    }
}
