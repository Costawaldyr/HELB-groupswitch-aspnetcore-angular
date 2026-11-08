using GroupSwitch.Business.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Tests.Scraping
{
    public class FakeHyperplanningScraper :IHyperplanningScraper
    {
        private const string FailureMessage = "Fake scraper failure";

        private readonly List<WeekResponse> _responses;
        private readonly bool _failAtTheEnd;

        public FakeHyperplanningScraper(IEnumerable<WeekResponse> responses, bool failAtTheEnd = false)
        {
            _responses = responses.ToList();
            _failAtTheEnd = failAtTheEnd;
        }

        public async IAsyncEnumerable<WeekResponse> ScrapeAsync(Func<string, bool> isGroupWanted, Func<int, bool> isWeekWanted)
        {
            var wanted = _responses.Where(response => isGroupWanted(response.GroupName) && isWeekWanted(response.Week));

            foreach (var response in wanted)
            {
                await Task.Yield();
                yield return response;
            }

            if (_failAtTheEnd)
            {
                throw new InvalidOperationException(FailureMessage);
            }
        }
    }
}
