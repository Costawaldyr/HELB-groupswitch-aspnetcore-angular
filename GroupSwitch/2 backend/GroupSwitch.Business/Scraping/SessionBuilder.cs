using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Scraping
{
    public class SessionBuilder : ISessionBuilder
    {
        private readonly SessionTimeCalculator _calculator;

        public SessionBuilder(SessionTimeCalculator calculator)
        {
            _calculator = calculator;
        }

        public IEnumerable<ScheduledSession> Build(IEnumerable<ParsedSession> parsedSessions, Func<int, bool> isWeekWanted)
        {
            var wantedSessions = parsedSessions.Where(parsed => isWeekWanted(parsed.Week));

            foreach (var parsed in wantedSessions)
            {
                yield return ToScheduledSession(parsed);
            }
        }

        private ScheduledSession ToScheduledSession(ParsedSession parsed)
        {
            var start = _calculator.StartOf(parsed.Week, parsed.Position);

            return new ScheduledSession
            {
                WeekNumber = parsed.Week,
                Start = start,
                End = _calculator.EndOf(start, parsed.DurationInSlots),
                Room = parsed.Room,
                Teacher = parsed.Teacher,
                Remark = parsed.Remark,
                IsCancelled = parsed.IsCancelled,
                Activity = new Activity { Title = parsed.Title }
            };
        }
    }
}
