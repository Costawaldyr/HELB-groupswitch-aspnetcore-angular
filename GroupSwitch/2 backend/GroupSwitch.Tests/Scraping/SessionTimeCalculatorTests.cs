using GroupSwitch.Business.Scraping;


namespace GroupSwitch.Tests.Scraping
{
    public class SessionTimeCalculatorTests
    {
        private readonly SessionTimeCalculator calculator = new(new DateTime(2026, 9, 14));

        [Fact]
        public void StartOf_FirstWeekFirstSlot_IsMondayAtEight()
        {
            var start = calculator.StartOf(1, 0);

            Assert.Equal(new DateTime(2026, 9, 14, 8, 0, 0), start);
        }

        [Fact]
        public void StartOf_WeekEightTuesdaySlotFour_IsNineOClock()
        {
            var start = calculator.StartOf(8, 56 + 4);

            Assert.Equal(new DateTime(2026, 11, 3, 9, 0, 0), start);
        }

        [Fact]
        public void EndOf_FourSlots_AddsOneHour()
        {
            var start = new DateTime(2026, 11, 3, 9, 0, 0);

            Assert.Equal(new DateTime(2026, 11, 3, 10, 0, 0), calculator.EndOf(start, 4));
        }
    }
}
