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
        private readonly int? _failAfterRetrievalCount;

        public FakeHyperplanningScraper(IEnumerable<WeekRetrieval> retrievals, bool failAtTheEnd = false, int? failAfterRetrievalCount = null)
        {
            _retrievals = retrievals.ToList();
            _failAtTheEnd = failAtTheEnd;
            _failAfterRetrievalCount = failAfterRetrievalCount;
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
            var yieldedCount = 0;

            foreach (var retrieval in wanted)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (yieldedCount == _failAfterRetrievalCount)
                {
                    throw new InvalidOperationException(FailureMessage);
                }

                yieldedCount++;
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
