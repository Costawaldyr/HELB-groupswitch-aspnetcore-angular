using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace GroupSwitch.Business.Scraping
{
    public class PlaywrightHyperplanningScraper : IHyperplanningScraper
    {
        private const string PostMethod = "POST";
        private const string GroupComboButton = ".ocb_bouton";
        private const string SelectedWeek = ".calendrier-jour.selected";
        private const string WeeksWithContent = ".calendrier-jour.avecContenu";
        private const string GroupComboContainer = ".ocb_cont";
        private const int ComboOpenWaitMs = 1500;
        private const int NotAWeek = 0;

        private readonly ResponseClassifier classifier = new();
        private readonly HyperplanningSettings _settings;
        private readonly IGroupListParser _groupListParser;

        public PlaywrightHyperplanningScraper(IGroupListParser groupListParser, IOptions<HyperplanningSettings> settings)
        {
            _groupListParser = groupListParser;
            _settings = settings.Value;
        }

        public async IAsyncEnumerable<WeekResponse> ScrapeAsync(Func<string, bool> isGroupWanted, Func<int, bool> isWeekWanted)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = _settings.Headless });
            var page = await browser.NewPageAsync();
            page.SetDefaultTimeout(_settings.TimeoutMs);

            await page.GotoAsync(_settings.PageUrl);

            var groupListJson = await WaitForBodyAsync(page, () => OpenGroupListFirstTimeAsync(page), classifier.IsGroupList);

            var groupNames = _groupListParser
                .ParseNames(groupListJson)
                .Where(isGroupWanted)
                .ToList();

            foreach (var groupName in groupNames)
            {
                var firstJson = await WaitForBodyAsync(page, () => SelectGroupAsync(page, groupName), classifier.IsSchedule);
                var selectedWeek = await SelectedWeekAsync(page);

                if (isWeekWanted(selectedWeek))
                {
                    yield return new WeekResponse(groupName, selectedWeek, firstJson);
                }

                var otherWeeks = (await WeeksWithContentAsync(page))
                    .Where(week => week != selectedWeek && isWeekWanted(week))
                    .ToList();

                foreach (var week in otherWeeks)
                {
                    var json = await WaitForBodyAsync(page, () => ClickWeekAsync(page, week), classifier.IsSchedule);
                    yield return new WeekResponse(groupName, week, json);
                }
            }
        }

        private async Task<string> WaitForBodyAsync(IPage page, Func<Task> action, Func<string, bool> isWanted)
        {
            var found = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<IResponse> onResponse = async (_, response) =>
            {
                if (response.Request.Method != PostMethod)
                {
                    return;
                }

                var body = await ReadBodyAsync(response);

                if (isWanted(body))
                {
                    found.TrySetResult(body);
                }
            };

            page.Response += onResponse;

            try
            {
                await action();

                return await found.Task.WaitAsync(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
            }
            finally
            {
                page.Response -= onResponse;
            }
        }

        private async Task<string> ReadBodyAsync(IResponse response)
        {
            try
            {
                return await response.TextAsync();
            }
            catch (PlaywrightException)
            {
                return string.Empty;
            }
        }

        private async Task OpenGroupListFirstTimeAsync(IPage page)
        {
            await page.Locator(GroupComboContainer).First.ClickAsync();
            await page.Locator(GroupComboButton).First.ClickAsync();
            await page.Locator(GroupComboButton).First.ClickAsync();
        }

        private async Task<bool> IsVisibleSoonAsync(ILocator locator)
        {
            try
            {
                await locator.WaitForAsync(new() { Timeout = ComboOpenWaitMs });

                return true;
            }
            catch (PlaywrightException)
            {
                return false;
            }
        }
        private async Task SelectGroupAsync(IPage page, string groupName)
        {
            var option = page.GetByRole(AriaRole.Option, new() { Name = groupName, Exact = true });

            if (!await IsVisibleSoonAsync(option))
            {
                await page.Locator(GroupComboButton).First.ClickAsync();
            }

            await option.ClickAsync();
        }

        private Task ClickWeekAsync(IPage page, int week)
        {
            return page.GetByRole(AriaRole.Option, new() { Name = $"Semaine {week}," }).ClickAsync();
        }

        private async Task<int> SelectedWeekAsync(IPage page)
        {
            var text = await page.Locator(SelectedWeek).First.InnerTextAsync();

            return int.Parse(text);
        }

        private async Task<List<int>> WeeksWithContentAsync(IPage page)
        {
            var texts = await page.Locator(WeeksWithContent).AllInnerTextsAsync();

            return texts
                .Select(text => int.TryParse(text, out var week) ? week : NotAWeek)
                .Where(week => week != NotAWeek)
                .ToList();
        }
    }
}