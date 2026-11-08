using GroupSwitch.Business.Models;

namespace GroupSwitch.Tests.Models
{
    public class ImportTotalsTests
    {
        [Fact]
        public void AddGroup_CountsGroupsAndSessions()
        {
            var totals = new ImportTotals();

            totals.AddGroup(24);
            totals.AddGroup(15);

            Assert.Equal(2, totals.Groups);
            Assert.Equal(39, totals.Sessions);
        }
    }
}