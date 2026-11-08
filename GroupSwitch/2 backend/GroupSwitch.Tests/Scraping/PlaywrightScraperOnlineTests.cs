using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;
using Microsoft.Extensions.Options;


namespace GroupSwitch.Tests.Scraping
{
    [Trait("Category", "Online")]
    public class PlaywrightScraperOnlineTests
    {
        [Fact]
        public async Task ScrapeAsync_OneGroupTwoWeeks_ReturnsRealScheduleJson()
        {
            var settings = Options.Create(new HyperplanningSettings
            {
                PageUrl = "https://planning.helb-prigogine.be/2026-2027/invite",
                Headless = true
            });
            var scraper = new PlaywrightHyperplanningScraper(new GroupListParser(), settings);
            var responses = new List<WeekResponse>();

            await foreach (var response in scraper.ScrapeAsync(name => name == "IODA B3", week => week == 8 || week == 11))
            {
                responses.Add(response);
            }

            var classifier = new ResponseClassifier();

            Assert.Equal(new[] { 8, 11 }, responses.Select(response => response.Week).OrderBy(week => week));
            Assert.All(responses, response => Assert.Equal("IODA B3", response.GroupName));
            Assert.All(responses, response => Assert.True(classifier.IsSchedule(response.Json)));

            var weekEleven = responses.Single(response => response.Week == 11);
            Assert.NotEmpty(new WeekScheduleParser().Parse(weekEleven.Json));
        }
    }
}
