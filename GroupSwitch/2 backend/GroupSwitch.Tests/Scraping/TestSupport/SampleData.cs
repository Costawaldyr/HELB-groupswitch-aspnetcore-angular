using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Tests.Scraping.TestSupport
{
    public class SampleData
    {
        public static List<WeekRetrieval> TwoGroupsResponses()
        {
            var week8Json = FixtureReader.Read("week8.json");
            var week11Json = FixtureReader.Read("week11.json");

            return new List<WeekRetrieval>
            {
                WeekRetrieval.Succeeded("IODA B3", 8, week8Json),
                WeekRetrieval.Succeeded("IODA B3", 11, week11Json),
                WeekRetrieval.Succeeded("SI 3", 11, week11Json)
            };
        }
    }
}
