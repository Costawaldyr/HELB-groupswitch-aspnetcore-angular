using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Scraping.Conversion
{
    public class SessionTimeCalculator
    {
        public const int SlotsPerDay = 56;
        public const int SlotMinutes = 15;
        public const int DayStartHour = 8;
        public const int DaysPerWeek = 7;

        private readonly DateTime firstMonday;

        public SessionTimeCalculator(DateTime _firstMonday)
        {
            firstMonday = _firstMonday;
        }

        public DateTime StartOf(int week, int position)
        {
            var day = position / SlotsPerDay;
            var slot = position % SlotsPerDay;
            var monday = firstMonday.AddDays(DaysPerWeek * (week - 1));
            return monday.AddDays(day).AddHours(DayStartHour).AddMinutes(SlotMinutes * slot);
        }

        public DateTime EndOf(DateTime start, int durationInSlots)
        {
            return start.AddMinutes(SlotMinutes * durationInSlots);
        }
    }
}
