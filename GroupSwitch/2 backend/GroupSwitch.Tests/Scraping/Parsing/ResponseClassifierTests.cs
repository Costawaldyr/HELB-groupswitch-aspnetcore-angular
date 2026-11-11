using GroupSwitch.Tests.Scraping.TestSupport;
using GroupSwitch.Business.Scraping.Parsing;

namespace GroupSwitch.Tests.Scraping.Parsing
{
    public class ResponseClassifierTests
    {
        private readonly ResponseClassifier _classifier = new();

        [Fact]
        public void FunctionNameOf_RealWeekEleven_IsSchedule()
        {
            var body = FixtureReader.Read("week11.json");

            Assert.Equal("FonctionEmploiDuTemps", _classifier.FunctionNameOf(body));
            Assert.True(_classifier.IsSchedule(body));
            Assert.False(_classifier.IsGroupList(body));
        }

        [Fact]
        public void IsGroupList_GroupListResponse_IsTrue()
        {
            var body = """{"nom":"FonctionRenvoyerListeDeRessource","donneesSec":{"donnees":{}}}""";

            Assert.True(_classifier.IsGroupList(body));
            Assert.False(_classifier.IsSchedule(body));
        }

        [Theory]
        [InlineData("<html><body>Hello</body></html>")]
        [InlineData("")]
        [InlineData("[]")]
        [InlineData("{}")]
        [InlineData("{\"autre\":1}")]
        public void FunctionNameOf_NotAHyperplanningResponse_ReturnsEmpty(string body)
        {
            Assert.Equal(string.Empty, _classifier.FunctionNameOf(body));
        }
    }
}
