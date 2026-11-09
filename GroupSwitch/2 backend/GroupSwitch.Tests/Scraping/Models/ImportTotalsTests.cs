using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Tests.Scraping.Models
{
    public class ImportTotalsTests
    {
        [Fact]
        public void AddSavedWeek_CountsGroupsAndSessions()
        {
            var totals = new ImportTotals();

            totals.AddSavedWeek("IODA B3", 24);
            totals.AddSavedWeek("SI 3", 15);

            Assert.Equal(2, totals.Groups);
            Assert.Equal(39, totals.Sessions);
        }

        [Fact]
        public void AddSavedWeek_SameGroupTwice_CountsOneGroupAndTwoWeeks()
        {
            var totals = new ImportTotals();

            totals.AddSavedWeek("IODA B3", 9);
            totals.AddSavedWeek("IODA B3", 15);
            totals.AddRejectedWeek();

            var summary = Assert.Single(totals.GroupSummaries);
            Assert.Equal(1, totals.Groups);
            Assert.Equal(2, totals.SuccessfulWeeks);
            Assert.Equal(1, totals.RejectedWeeks);
            Assert.Equal(2, summary.WeekCount);
            Assert.Equal(24, summary.SessionCount);
        }
    }
}
