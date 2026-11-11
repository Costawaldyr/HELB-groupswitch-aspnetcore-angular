using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IHyperplanningScraper
    {
        Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken cancellationToken);

        IAsyncEnumerable<WeekRetrieval> ScrapeAsync(
            IReadOnlyCollection<string> groupNames,
            IReadOnlyCollection<int> weeks,
            CancellationToken cancellationToken
        );
    }
}
