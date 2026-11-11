using GroupSwitch.Business.Scraping.Parsing;

namespace GroupSwitch.Tests.Scraping.Parsing
{
    public class ScheduleRequestParserTests
    {
        private readonly ScheduleRequestParser _parser = new();

        private string BodyFor(string groupName, string domain)
        {
            var body = "{'donneesSec':{'_Signature_':{'listeRecherche':[{'L':'" + groupName
                + "'}]},'donnees':{'Domaine':{'_T':8,'V':'" + domain + "'}}}}";

            return body.Replace('\'', '"');
        }

        [Fact]
        public void Parse_OneWeekRequest_ReturnsGroupAndWeek()
        {
            var request = _parser.Parse(BodyFor("IODA B3", "[8]"));

            Assert.NotNull(request);
            Assert.Equal("IODA B3", request.GroupName);
            Assert.Equal(8, request.Week);
        }

        [Fact]
        public void Parse_WeekRange_ReturnsNoWeek()
        {
            var request = _parser.Parse(BodyFor("IODA B3", "[7..9]"));

            Assert.NotNull(request);
            Assert.Null(request.Week);
        }

        [Fact]
        public void Parse_NotJson_ReturnsNull()
        {
            Assert.Null(_parser.Parse("not json"));
        }

        [Fact]
        public void Parse_NullOrEmptyBody_ReturnsNull()
        {
            Assert.Null(_parser.Parse(null));
            Assert.Null(_parser.Parse(string.Empty));
        }

        [Fact]
        public void Parse_NoSearchedGroup_ReturnsNull()
        {
            var body = "{'donneesSec':{'_Signature_':{'listeRecherche':[]},'donnees':{}}}".Replace('\'', '"');

            Assert.Null(_parser.Parse(body));
        }
    }
}
