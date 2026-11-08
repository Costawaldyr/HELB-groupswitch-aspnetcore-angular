using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;
using GroupSwitch.Business.Services;
using GroupSwitch.Tests.Utils;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GroupSwitch.Tests.Services
{
    public class WeekResponseConverterTests
    {
        private readonly WeekResponseConverter converter = TestObjects.NewConverter();
        private readonly WeekResponse weekEleven = new("IODA B3", 11, FixtureReader.Read("week11.json"));

        [Fact]
        public void Convert_WantedWeek_ReturnsAllSessions()
        {
            var sessions = converter.Convert(weekEleven, QuadrimesterFilters.IsInFirstQuadrimester);

            Assert.Equal(15, sessions.Count());
        }

        [Fact]
        public void Convert_UnwantedWeek_ReturnsNothing()
        {
            var sessions = converter.Convert(weekEleven, week => week == 8);

            Assert.Empty(sessions);
        }
    }
}