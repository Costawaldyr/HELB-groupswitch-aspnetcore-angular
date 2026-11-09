using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Services;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GroupSwitch.Tests.Scraping.Services
{
    public class WeekResponseConverterServiceTests
    {
        private readonly WeekResponseConverterService weekResponseConverterService = TestObjects.NewWeekResponseConverterService();
        private readonly WeekRetrieval weekEleven = WeekRetrieval.Succeeded("IODA B3", 11, FixtureReader.Read("week11.json"));

        [Fact]
        public void Convert_WantedWeek_ReturnsAllSessions()
        {
            var sessions = weekResponseConverterService.Convert(weekEleven, week => true);

            Assert.Equal(15, sessions.Count());
        }

        [Fact]
        public void Convert_UnwantedWeek_ReturnsNothing()
        {
            var sessions = weekResponseConverterService.Convert(weekEleven, week => week == 8);

            Assert.Empty(sessions);
        }
    }
}