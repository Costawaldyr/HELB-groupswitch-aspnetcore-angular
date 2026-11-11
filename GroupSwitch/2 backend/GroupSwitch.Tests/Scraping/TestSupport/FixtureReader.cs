using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Tests.Scraping.TestSupport
{
    public class FixtureReader
    {
        public static string Read(string fileName)
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scraping", "Fixtures", fileName));
        }
    }
}
