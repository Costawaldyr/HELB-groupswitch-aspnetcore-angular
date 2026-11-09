using System.Diagnostics.CodeAnalysis;

namespace GroupSwitch.Business.Scraping.Models
{
    public record WeekRetrieval(
        string GroupName,
        int RequestedWeek,
        string? Json,
        string? RequestBody,
        DateTime RetrievedAtUtc,
        string? FailureReason)
    {
        [MemberNotNullWhen(true, nameof(Json))]
        public bool IsSuccessful => Json is not null;

        public static WeekRetrieval Succeeded(string groupName, int requestedWeek, string json, string? requestBody = null)
        {
            return new WeekRetrieval(groupName, requestedWeek, json, requestBody, DateTime.UtcNow, null);
        }

        public static WeekRetrieval Failed(string groupName, int requestedWeek, string failureReason)
        {
            return new WeekRetrieval(groupName, requestedWeek, null, null, DateTime.UtcNow, failureReason);
        }
    }
}
