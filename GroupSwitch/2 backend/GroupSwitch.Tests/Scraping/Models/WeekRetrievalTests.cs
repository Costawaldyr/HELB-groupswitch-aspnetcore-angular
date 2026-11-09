using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Tests.Scraping.Models
{
    public class WeekRetrievalTests
    {
        [Fact]
        public void Succeeded_HasJsonAndNoFailureReason()
        {
            var retrieval = WeekRetrieval.Succeeded("IODA B3", 8, "{}", "body");

            Assert.True(retrieval.IsSuccessful);
            Assert.Equal("{}", retrieval.Json);
            Assert.Null(retrieval.FailureReason);
            Assert.Equal("body", retrieval.RequestBody);
        }

        [Fact]
        public void Failed_HasNoJsonAndAReason()
        {
            var retrieval = WeekRetrieval.Failed("IODA B3", 7, "Timeout");

            Assert.False(retrieval.IsSuccessful);
            Assert.Null(retrieval.Json);
            Assert.Equal("Timeout", retrieval.FailureReason);
        }

        [Fact]
        public void Succeeded_RetrievedAtIsInUtc()
        {
            var retrieval = WeekRetrieval.Succeeded("IODA B3", 8, "{}");

            Assert.Equal(DateTimeKind.Utc, retrieval.RetrievedAtUtc.Kind);
        }
    }
}
