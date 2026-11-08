using GroupSwitch.Business.Scraping;
using GroupSwitch.Business.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Tests.Utils
{
    public static class TestObjects
    {
        public static readonly DateTime FirstMonday = new(2026, 9, 14);

        public static SessionBuilder NewSessionBuilder()
        {
            return new SessionBuilder(new SessionTimeCalculator(FirstMonday));
        }

        public static WeekResponseConverter NewConverter()
        {
            return new WeekResponseConverter(new WeekScheduleParser(), NewSessionBuilder());
        }
    }
}
