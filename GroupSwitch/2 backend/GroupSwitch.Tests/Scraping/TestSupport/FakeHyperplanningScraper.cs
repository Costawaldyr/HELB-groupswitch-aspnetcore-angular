using System.Runtime.CompilerServices;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Tests.Scraping.TestSupport
{
    public class FakeHyperplanningScraper : IHyperplanningScraper
    {
        private const string FailureMessage = "Fake scraper failure";

        private readonly List<WeekRetrieval> _retrievals;
        private readonly bool _failAtTheEnd;

        public FakeHyperplanningScraper(IEnumerable<WeekRetrieval> retrievals, bool failAtTheEnd = false)
        {
            _retrievals = retrievals.ToList();
            _failAtTheEnd = failAtTheEnd;
        }

        public Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<string> groupNames = _retrievals.Select(retrieval => retrieval.GroupName).Distinct().ToList();

            return Task.FromResult(groupNames);
        }

        public async IAsyncEnumerable<WeekRetrieval> ScrapeAsync(
            IReadOnlyCollection<string> groupNames,
            IReadOnlyCollection<int> weeks,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var wanted = _retrievals.Where(retrieval => groupNames.Contains(retrieval.GroupName) && weeks.Contains(retrieval.RequestedWeek));

            foreach (var retrieval in wanted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return retrieval;
            }

            if (_failAtTheEnd)
            {
                throw new InvalidOperationException(FailureMessage);
            }
        }
    }
}
