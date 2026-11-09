using GroupSwitch.Business.Scraping.Conversion;
using GroupSwitch.Business.Scraping.Parsing;
using GroupSwitch.Business.Scraping.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Tests.Scraping.TestSupport
{
    public static class TestObjects
    {
        public static readonly DateTime FirstMonday = new(2026, 9, 14);

        public static SessionBuilder NewSessionBuilder()
        {
            return new SessionBuilder(new SessionTimeCalculator(FirstMonday));
        }

        public static WeekResponseConverterService NewWeekResponseConverterService()
        {
            return new WeekResponseConverterService(new WeekScheduleParser(), NewSessionBuilder());
        }
    }
}
