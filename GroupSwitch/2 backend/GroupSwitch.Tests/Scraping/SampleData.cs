using GroupSwitch.Business.Interfaces;
using GroupSwitch.Tests.Utils;


namespace GroupSwitch.Tests.Scraping
{
    public class SampleData
    {
        public static List<WeekResponse> TwoGroupsResponses()
        {
            var week8Json = FixtureReader.Read("week8.json");
            var week11Json = FixtureReader.Read("week11.json");

            return new List<WeekResponse>
            {
                new("IODA B3", 8, week8Json),
                new("IODA B3", 11, week11Json),
                new("SI 3", 11, week11Json)
            };
        }
    }
}
