using GroupSwitch.Business.Scraping.Parsing;

namespace GroupSwitch.Tests.Scraping.Parsing
{
    public class GroupListParserTests
    {
        private readonly GroupListParser _parser = new();

        [Fact]
        public void ParseNames_TwoGroups_ReturnsBothNames()
        {
            var json = """
                {"donneesSec":{"donnees":{"ListeRessources":{"Liste":[
                    {"L":"ES B1","N":"51#a","G":1},
                    {"L":"IODA B3","N":"51#b","G":1}
                ]}}}}
                """;

            var names = _parser.ParseNames(json).ToList();

            Assert.Equal(new[] { "ES B1", "IODA B3" }, names);
        }
    }
}
