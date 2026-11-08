using GroupSwitch.Business.Scraping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Tests.Scraping
{
    public class RealHyperplanningFilesTests
    {
        private readonly WeekScheduleParser _parser = new();

        private string ReadFixture(string name)
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
        }

        [Fact]
        public void Parse_RealWeekEleven_Returns15Sessions()
        {
            var sessions = _parser.Parse(ReadFixture("week11.json")).ToList();

            Assert.Equal(15, sessions.Count);
            Assert.All(sessions, session => Assert.Equal(11, session.Week));
            Assert.DoesNotContain(sessions, session => session.IsCancelled);
        }

        [Fact]
        public void Parse_RealWeekEight_HasOneCancelledSession()
        {
            var sessions = _parser.Parse(ReadFixture("week8.json")).ToList();

            Assert.Equal(9, sessions.Count);
            Assert.Single(sessions, session => session.IsCancelled);
        }
    }
}
