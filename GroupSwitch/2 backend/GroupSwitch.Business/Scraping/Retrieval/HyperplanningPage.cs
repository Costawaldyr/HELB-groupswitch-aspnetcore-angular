using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Parsing;
using Microsoft.Playwright;

namespace GroupSwitch.Business.Scraping.Retrieval
{
    public class HyperplanningPage
    {
        private const string PostMethod = "POST";
        private const string GroupComboContainer = ".ocb_cont";
        private const string GroupComboButton = ".ocb_bouton";
        private const string SelectedWeek = ".calendrier-jour.selected";
        private const int ElementWaitMs = 1500;
        private const int LabelPollMs = 100;
        private const string NoResponseMessage = "Aucune réponse de Hyperplanning avant la fin du délai.";

        private readonly IPage _page;
        private readonly ResponseClassifier _classifier;
        private readonly ScheduleRequestParser _requestParser = new();
        private readonly int _timeoutMs;

        public HyperplanningPage(IPage page, ResponseClassifier classifier, int timeoutMs)
        {
            _page = page;
            _classifier = classifier;
            _timeoutMs = timeoutMs;
        }

        public async Task OpenAsync(string pageUrl)
        {
            await _page.GotoAsync(pageUrl);
        }

        public async Task<string> OpenGroupListAsync(CancellationToken cancellationToken)
        {
            var captured = await WaitForResponseAsync(
                OpenGroupComboAsync,
                response => _classifier.IsGroupList(response.Body),
                cancellationToken);

            return captured.Body;
        }

        public Task<CapturedResponse> SelectGroupAsync(string groupName, CancellationToken cancellationToken)
        {
            return WaitForResponseAsync(
                () => ClickGroupOptionAsync(groupName),
                response => IsScheduleOf(response, groupName, null),
                cancellationToken);
        }

        public async Task<bool> IsGroupDisplayedAsync(string groupName)
        {
            var expectedName = Normalize(groupName);
            var deadline = DateTime.UtcNow.AddMilliseconds(ElementWaitMs);

            while (true)
            {
                var displayedText = await _page.Locator(GroupComboContainer).First.InnerTextAsync();

                if (Normalize(displayedText) == expectedName)
                {
                    return true;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    return false;
                }

                await Task.Delay(LabelPollMs);
            }
        }

        public async Task<int?> SelectedWeekAsync()
        {
            var text = await _page.Locator(SelectedWeek).First.InnerTextAsync();

            return int.TryParse(text, out var week) ? week : null;
        }

        public async Task<bool> HasWeekAsync(int week)
        {
            return await IsVisibleSoonAsync(WeekOption(week));
        }

        public Task<CapturedResponse> SelectWeekAsync(string groupName, int week, CancellationToken cancellationToken)
        {
            return WaitForResponseAsync(
                () => WeekOption(week).ClickAsync(),
                response => IsScheduleOf(response, groupName, week),
                cancellationToken);
        }

        private bool IsScheduleOf(CapturedResponse response, string groupName, int? week)
        {
            if (!_classifier.IsSchedule(response.Body))
            {
                return false;
            }

            var request = _requestParser.Parse(response.RequestBody);

            return request is not null
                && request.GroupName == groupName
                && (week is null || request.Week == week);
        }

        private ILocator WeekOption(int week)
        {
            return _page.GetByRole(AriaRole.Option, new() { Name = $"Semaine {week}," });
        }

        private async Task OpenGroupComboAsync()
        {
            await _page.Locator(GroupComboContainer).First.ClickAsync();
            await _page.Locator(GroupComboButton).First.ClickAsync();
            await _page.Locator(GroupComboButton).First.ClickAsync();
        }

        private async Task ClickGroupOptionAsync(string groupName)
        {
            var option = _page.GetByRole(AriaRole.Option, new() { Name = groupName, Exact = true });

            if (!await IsVisibleSoonAsync(option))
            {
                await _page.Locator(GroupComboButton).First.ClickAsync();
            }

            await option.ClickAsync();
        }

        private async Task<bool> IsVisibleSoonAsync(ILocator locator)
        {
            try
            {
                await locator.WaitForAsync(new() { Timeout = ElementWaitMs });

                return true;
            }
            catch (PlaywrightException)
            {
                return false;
            }
        }

        private async Task<CapturedResponse> WaitForResponseAsync(
            Func<Task> action,
            Func<CapturedResponse, bool> isWanted,
            CancellationToken cancellationToken)
        {
            var found = new TaskCompletionSource<CapturedResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<IResponse> onResponse = async (_, response) =>
            {
                if (response.Request.Method != PostMethod)
                {
                    return;
                }

                var body = await ReadBodyAsync(response);
                var captured = new CapturedResponse(body, response.Request.PostData);

                if (isWanted(captured))
                {
                    found.TrySetResult(captured);
                }
            };

            _page.Response += onResponse;

            try
            {
                await action();

                return await found.Task.WaitAsync(TimeSpan.FromMilliseconds(_timeoutMs), cancellationToken);
            }
            catch (System.TimeoutException)
            {
                throw new System.TimeoutException(NoResponseMessage);
            }
            finally
            {
                _page.Response -= onResponse;
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

        private string Normalize(string value)
        {
            return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
