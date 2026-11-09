using System.Runtime.CompilerServices;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Parsing;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace GroupSwitch.Business.Scraping.Retrieval
{
    public class PlaywrightHyperplanningScraper : IHyperplanningScraper
    {
        private const string GroupNotDisplayedMessage = "Le groupe affiché ne correspond pas au groupe demandé.";
        private const string UnreadableWeekMessage = "La semaine affichée est illisible.";
        private const string WeekNotInCalendarMessage = "La semaine n'existe pas dans le calendrier.";
        private const string WeekNotDisplayedFormat = "La semaine affichée ({0}) ne correspond pas à la semaine demandée.";
        private const char LineSeparator = '\n';

        private readonly ResponseClassifier _classifier = new();
        private readonly HyperplanningSettings _settings;
        private readonly IGroupListParser _groupListParser;

        public PlaywrightHyperplanningScraper(IGroupListParser groupListParser, IOptions<HyperplanningSettings> settings)
        {
            _groupListParser = groupListParser;
            _settings = settings.Value;
        }

        public async Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken cancellationToken)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await LaunchBrowserAsync(playwright);
            var hyperplanningPage = await OpenPageAsync(browser);

            var groupListJson = await hyperplanningPage.OpenGroupListAsync(cancellationToken);

            return _groupListParser.ParseNames(groupListJson).ToList();
        }

        public async IAsyncEnumerable<WeekRetrieval> ScrapeAsync(
            IReadOnlyCollection<string> groupNames,
            IReadOnlyCollection<int> weeks,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var sortedWeeks = weeks.Distinct().OrderBy(week => week).ToList();

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await LaunchBrowserAsync(playwright);
            var hyperplanningPage = await OpenPageAsync(browser);

            await hyperplanningPage.OpenGroupListAsync(cancellationToken);

            foreach (var groupName in groupNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrievals = await RetrieveGroupAsync(hyperplanningPage, groupName, sortedWeeks, cancellationToken);

                foreach (var retrieval in retrievals)
                {
                    yield return retrieval;
                }
            }
        }

        private Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
        {
            return playwright.Chromium.LaunchAsync(new() { Headless = _settings.Headless });
        }

        private async Task<HyperplanningPage> OpenPageAsync(IBrowser browser)
        {
            var page = await browser.NewPageAsync();
            page.SetDefaultTimeout(_settings.TimeoutMs);

            var hyperplanningPage = new HyperplanningPage(page, _classifier, _settings.TimeoutMs);
            await hyperplanningPage.OpenAsync(_settings.PageUrl);

            return hyperplanningPage;
        }

        private async Task<List<WeekRetrieval>> RetrieveGroupAsync(
            HyperplanningPage hyperplanningPage,
            string groupName,
            IReadOnlyList<int> weeks,
            CancellationToken cancellationToken)
        {
            CapturedResponse groupResponse;
            int? displayedWeek;

            try
            {
                await Task.Delay(_settings.DelayBetweenRequestsMs, cancellationToken);

                groupResponse = await hyperplanningPage.SelectGroupAsync(groupName, cancellationToken);

                if (!await hyperplanningPage.IsGroupDisplayedAsync(groupName))
                {
                    return FailedWeeks(groupName, weeks, GroupNotDisplayedMessage);
                }

                displayedWeek = await hyperplanningPage.SelectedWeekAsync();
            }
            catch (Exception exception) when (IsRetrievalFailure(exception))
            {
                return FailedWeeks(groupName, weeks, ReasonOf(exception));
            }

            if (displayedWeek is null)
            {
                return FailedWeeks(groupName, weeks, UnreadableWeekMessage);
            }

            var retrievals = new List<WeekRetrieval>();

            foreach (var week in weeks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (week == displayedWeek)
                {
                    retrievals.Add(WeekRetrieval.Succeeded(groupName, week, groupResponse.Body, groupResponse.RequestBody));
                }
                else
                {
                    retrievals.Add(await RetrieveWeekAsync(hyperplanningPage, groupName, week, cancellationToken));
                }
            }

            return retrievals;
        }

        private async Task<WeekRetrieval> RetrieveWeekAsync(
            HyperplanningPage hyperplanningPage,
            string groupName,
            int week,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_settings.DelayBetweenRequestsMs, cancellationToken);

                if (!await hyperplanningPage.HasWeekAsync(week))
                {
                    return WeekRetrieval.Failed(groupName, week, WeekNotInCalendarMessage);
                }

                var captured = await hyperplanningPage.SelectWeekAsync(groupName, week, cancellationToken);
                var displayedWeek = await hyperplanningPage.SelectedWeekAsync();

                if (displayedWeek != week)
                {
                    return WeekRetrieval.Failed(groupName, week, string.Format(WeekNotDisplayedFormat, displayedWeek));
                }

                return WeekRetrieval.Succeeded(groupName, week, captured.Body, captured.RequestBody);
            }
            catch (Exception exception) when (IsRetrievalFailure(exception))
            {
                return WeekRetrieval.Failed(groupName, week, ReasonOf(exception));
            }
        }

        private List<WeekRetrieval> FailedWeeks(string groupName, IEnumerable<int> weeks, string reason)
        {
            return weeks.Select(week => WeekRetrieval.Failed(groupName, week, reason)).ToList();
        }

        private bool IsRetrievalFailure(Exception exception)
        {
            return exception is System.TimeoutException or PlaywrightException;
        }

        private string ReasonOf(Exception exception)
        {
            return exception.Message.Split(LineSeparator).First().Trim();
        }
    }
}
