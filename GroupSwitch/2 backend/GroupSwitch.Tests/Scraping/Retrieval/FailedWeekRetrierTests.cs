using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Retrieval;

namespace GroupSwitch.Tests.Scraping.Retrieval
{
    public class FailedWeekRetrierTests
    {
        private const string GroupName = "KINE 1T";
        private const string Json = "{}";
        private const string NotInCalendar = "La semaine n'existe pas dans le calendrier.";

        private readonly FailedWeekRetrier _retrier = new();
        private readonly List<IReadOnlyList<int>> _freshPageCalls = new();

        private bool IsRetryable(WeekRetrieval retrieval)
        {
            return retrieval.FailureReason != NotInCalendar;
        }

        private Task<List<WeekRetrieval>> SucceedInFreshPageAsync(IReadOnlyList<int> weeks)
        {
            _freshPageCalls.Add(weeks);

            return Task.FromResult(weeks.Select(week => WeekRetrieval.Succeeded(GroupName, week, Json)).ToList());
        }

        private Task<List<WeekRetrieval>> FailInFreshPageAsync(IReadOnlyList<int> weeks)
        {
            _freshPageCalls.Add(weeks);

            return Task.FromResult(weeks.Select(week => WeekRetrieval.Failed(GroupName, week, "Timeout again")).ToList());
        }

        [Fact]
        public async Task Retry_AllWeeksSucceeded_OpensNoFreshPage()
        {
            var retrievals = new[] { WeekRetrieval.Succeeded(GroupName, 1, Json), WeekRetrieval.Succeeded(GroupName, 2, Json) };

            var result = await _retrier.RetryAsync(retrievals, IsRetryable, SucceedInFreshPageAsync);

            Assert.Empty(_freshPageCalls);
            Assert.All(result, retrieval => Assert.True(retrieval.IsSuccessful));
        }

        [Fact]
        public async Task Retry_OneWeekFailed_RetriesOnlyThatWeekOnceAndKeepsTheOrder()
        {
            var retrievals = new[]
            {
                WeekRetrieval.Succeeded(GroupName, 11, Json),
                WeekRetrieval.Failed(GroupName, 12, "Timeout"),
                WeekRetrieval.Succeeded(GroupName, 13, Json)
            };

            var result = await _retrier.RetryAsync(retrievals, IsRetryable, SucceedInFreshPageAsync);

            var call = Assert.Single(_freshPageCalls);
            Assert.Equal(new[] { 12 }, call);
            Assert.Equal(new[] { 11, 12, 13 }, result.Select(retrieval => retrieval.RequestedWeek));
            Assert.All(result, retrieval => Assert.True(retrieval.IsSuccessful));
        }

        [Fact]
        public async Task Retry_TwoSeparateWeeksFailed_RetriesEachWeekInItsOwnFreshPage()
        {
            var retrievals = new[]
            {
                WeekRetrieval.Failed(GroupName, 12, "Timeout"),
                WeekRetrieval.Succeeded(GroupName, 13, Json),
                WeekRetrieval.Failed(GroupName, 14, "Timeout")
            };

            await _retrier.RetryAsync(retrievals, IsRetryable, SucceedInFreshPageAsync);

            Assert.Equal(2, _freshPageCalls.Count);
            Assert.Equal(new[] { 12 }, _freshPageCalls[0]);
            Assert.Equal(new[] { 14 }, _freshPageCalls[1]);
        }

        [Fact]
        public async Task Retry_WholeGroupFailed_RetriesAllItsWeeksInOneFreshPage()
        {
            var retrievals = new[] { WeekRetrieval.Failed(GroupName, 1, "Timeout"), WeekRetrieval.Failed(GroupName, 2, "Timeout") };

            var result = await _retrier.RetryAsync(retrievals, IsRetryable, SucceedInFreshPageAsync);

            var call = Assert.Single(_freshPageCalls);
            Assert.Equal(new[] { 1, 2 }, call);
            Assert.All(result, retrieval => Assert.True(retrieval.IsSuccessful));
        }

        [Fact]
        public async Task Retry_StillFailing_TriesOnlyOnceAndReportsTheNewFailure()
        {
            var retrievals = new[] { WeekRetrieval.Succeeded(GroupName, 11, Json), WeekRetrieval.Failed(GroupName, 12, "Timeout") };

            var result = await _retrier.RetryAsync(retrievals, IsRetryable, FailInFreshPageAsync);

            Assert.Single(_freshPageCalls);
            var retried = result.Single(retrieval => retrieval.RequestedWeek == 12);
            Assert.False(retried.IsSuccessful);
            Assert.Equal("Timeout again", retried.FailureReason);
        }

        [Fact]
        public async Task Retry_WeekAbsentFromTheCalendar_IsNotRetried()
        {
            var retrievals = new[] { WeekRetrieval.Succeeded(GroupName, 14, Json), WeekRetrieval.Failed(GroupName, 60, NotInCalendar) };

            var result = await _retrier.RetryAsync(retrievals, IsRetryable, SucceedInFreshPageAsync);

            Assert.Empty(_freshPageCalls);
            Assert.Equal(NotInCalendar, result.Single(retrieval => retrieval.RequestedWeek == 60).FailureReason);
        }
    }
}
