using System.Text.Json;
using GroupSwitch.Business.Scraping.Conversion;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Parsing;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Business.Scraping.Services
{
    public class WeekAssessmentService : IWeekAssessmentService
    {
        private const string UnknownFailureMessage = "Récupération échouée sans raison indiquée.";
        private const string NotAScheduleMessage = "La réponse n'est pas un emploi du temps lisible.";
        private const string RequestMismatchMessage = "La requête ne porte pas sur ce groupe et cette semaine.";
        private const string OtherWeekFormat = "La réponse contient des cours de la semaine {0} au lieu de la semaine {1}.";
        private const string OutsideTheGridMessage = "Un cours sort de la grille horaire (position ou durée invalide).";
        private const string MissingSubjectMessage = "Un cours n'a pas de matière.";
        private const string CompleteFormat = "{0} séances";
        private const string CompleteEmptyMessage = "Semaine vide vérifiée : aucun cours, aucune annulation.";

        private readonly ResponseClassifier _classifier = new();
        private readonly ScheduleRequestParser _requestParser = new();
        private readonly IWeekScheduleParser _parser;
        private readonly ISessionBuilder _sessionBuilder;

        public WeekAssessmentService(IWeekScheduleParser parser, ISessionBuilder sessionBuilder)
        {
            _parser = parser;
            _sessionBuilder = sessionBuilder;
        }

        public WeekAssessment Assess(WeekRetrieval retrieval)
        {
            if (!retrieval.IsSuccessful)
            {
                return Rejected(retrieval, WeekResponseStatus.RetrievalFailed, retrieval.FailureReason ?? UnknownFailureMessage);
            }

            if (retrieval.Json is null || !_classifier.IsSchedule(retrieval.Json))
            {
                return Rejected(retrieval, WeekResponseStatus.Invalid, NotAScheduleMessage);
            }

            List<ParsedSession> parsedSessions;

            try
            {
                parsedSessions = _parser.Parse(retrieval.Json).ToList();
            }
            catch (Exception exception) when (exception is HyperplanningFormatException or JsonException)
            {
                return Rejected(retrieval, WeekResponseStatus.Invalid, exception.Message);
            }

            if (!IsRequestOfThisWeek(retrieval))
            {
                return Rejected(retrieval, WeekResponseStatus.Uncorrelated, RequestMismatchMessage);
            }

            var otherWeekSession = parsedSessions.FirstOrDefault(session => session.Week != retrieval.RequestedWeek);

            if (otherWeekSession is not null)
            {
                var message = string.Format(OtherWeekFormat, otherWeekSession.Week, retrieval.RequestedWeek);

                return Rejected(retrieval, WeekResponseStatus.Uncorrelated, message);
            }

            if (parsedSessions.Any(IsOutsideTheGrid))
            {
                return Rejected(retrieval, WeekResponseStatus.Invalid, OutsideTheGridMessage);
            }

            if (parsedSessions.Any(session => string.IsNullOrWhiteSpace(session.Title)))
            {
                return Rejected(retrieval, WeekResponseStatus.Invalid, MissingSubjectMessage);
            }

            if (!parsedSessions.Any())
            {
                return new WeekAssessment(retrieval.GroupName, retrieval.RequestedWeek, WeekResponseStatus.CompleteEmpty, new List<ScheduledSession>(), CompleteEmptyMessage);
            }

            var sessions = _sessionBuilder.Build(parsedSessions, week => week == retrieval.RequestedWeek).ToList();

            return new WeekAssessment(retrieval.GroupName, retrieval.RequestedWeek, WeekResponseStatus.Complete, sessions, string.Format(CompleteFormat, sessions.Count));
        }

        private bool IsRequestOfThisWeek(WeekRetrieval retrieval)
        {
            if (retrieval.RequestBody is null)
            {
                return true;
            }

            var request = _requestParser.Parse(retrieval.RequestBody);

            return request is not null
                && request.GroupName == retrieval.GroupName
                && request.Week == retrieval.RequestedWeek;
        }

        private bool IsOutsideTheGrid(ParsedSession session)
        {
            var weekSlotCount = SessionTimeCalculator.DaysPerWeek * SessionTimeCalculator.SlotsPerDay;
            var slotInDay = session.Position % SessionTimeCalculator.SlotsPerDay;

            return session.Position < 0
                || session.Position >= weekSlotCount
                || session.DurationInSlots <= 0
                || slotInDay + session.DurationInSlots > SessionTimeCalculator.SlotsPerDay;
        }

        private WeekAssessment Rejected(WeekRetrieval retrieval, WeekResponseStatus status, string diagnostic)
        {
            return new WeekAssessment(retrieval.GroupName, retrieval.RequestedWeek, status, new List<ScheduledSession>(), diagnostic);
        }
    }
}
