using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Retrieval
{
    public class FailedWeekRetrier
    {
        public async Task<List<WeekRetrieval>> RetryAsync(
            IReadOnlyList<WeekRetrieval> retrievals,
            Func<WeekRetrieval, bool> isRetryable,
            Func<IReadOnlyList<int>, Task<List<WeekRetrieval>>> retrieveInFreshPageAsync)
        {
            var failedWeeks = retrievals
                .Where(retrieval => !retrieval.IsSuccessful && isRetryable(retrieval))
                .Select(retrieval => retrieval.RequestedWeek)
                .ToList();

            if (!failedWeeks.Any())
            {
                return retrievals.ToList();
            }

            var retried = new List<WeekRetrieval>();

            if (failedWeeks.Count == retrievals.Count)
            {
                retried.AddRange(await retrieveInFreshPageAsync(failedWeeks));
            }
            else
            {
                foreach (var week in failedWeeks)
                {
                    retried.AddRange(await retrieveInFreshPageAsync(new[] { week }));
                }
            }

            return retrievals
                .Select(original => retried.FirstOrDefault(retry => retry.RequestedWeek == original.RequestedWeek) ?? original)
                .ToList();
        }
    }
}
