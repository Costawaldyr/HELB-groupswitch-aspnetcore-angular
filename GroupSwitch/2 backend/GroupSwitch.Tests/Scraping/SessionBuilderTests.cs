using GroupSwitch.Business.Scraping;
using GroupSwitch.Tests.Utils;


namespace GroupSwitch.Tests.Scraping
{
    public class SessionBuilderTests
    {
        private readonly WeekScheduleParser _parser = new();
        private readonly SessionBuilder _builder = new(new SessionTimeCalculator(new DateTime(2026, 9, 14)));

        [Fact]
        public void Build_WeekEleven_FirstSessionIsWednesdayNineToTen()
        {
            var parsed = _parser.Parse(FixtureReader.Read("week11.json"));

            var first = _builder.Build(parsed, QuadrimesterFilters.IsInFirstQuadrimester).First();

            Assert.Equal("Dév. 4 : Plateforme.net", first.Activity!.Title);
            Assert.Equal(new DateTime(2026, 11, 25, 9, 0, 0), first.Start);
            Assert.Equal(new DateTime(2026, 11, 25, 10, 0, 0), first.End);
            Assert.Equal(11, first.WeekNumber);
        }

        [Fact]
        public void Build_WeekEleven_KeepsAllFifteenSessions()
        {
            var parsed = _parser.Parse(FixtureReader.Read("week11.json"));

            var sessions = _builder.Build(parsed, QuadrimesterFilters.IsInFirstQuadrimester).ToList();

            Assert.Equal(15, sessions.Count);
        }

        [Fact]
        public void Build_FilterWithLambda_KeepsOnlyWantedWeek()
        {
            var parsed = _parser.Parse(FixtureReader.Read("week11.json"));

            var sessions = _builder.Build(parsed, week => week == 8).ToList();

            Assert.Empty(sessions);
        }

        [Fact]
        public void Build_FilterWithClosure_UsesTheVariableOutsideTheLambda()
        {
            var parsed = _parser.Parse(FixtureReader.Read("week11.json"));
            var firstWantedWeek = 12;

            var sessions = _builder.Build(parsed, week => week >= firstWantedWeek).ToList();

            Assert.Empty(sessions);
        }

        [Fact]
        public void Build_CancelledSession_KeepsFlagAndComputesFridayAfternoon()
        {
            var parsed = _parser.Parse(FixtureReader.Read("week8.json"));

            var cancelled = _builder.Build(parsed, QuadrimesterFilters.IsInFirstQuadrimester).Single(session => session.IsCancelled);

            Assert.Equal("Programmation Réseaux", cancelled.Activity!.Title);
            Assert.Equal(new DateTime(2026, 11, 6, 15, 30, 0), cancelled.Start);
            Assert.Equal(new DateTime(2026, 11, 6, 17, 30, 0), cancelled.End);
            Assert.Equal("Panne de chauffage", cancelled.Remark);
        }
    }
}
