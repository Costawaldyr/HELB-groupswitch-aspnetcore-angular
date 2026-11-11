namespace GroupSwitch.Business.Scraping.Models
{
    public class HyperplanningSettings
    {
        public const string SectionName = "Hyperplanning";
        public const int DefaultTimeoutMs = 30000;
        public const int DefaultDelayBetweenRequestsMs = 500;
        public const int DefaultFirstWeek = 1;
        public const int DefaultLastWeek = 14;

        public string PageUrl { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime FirstMonday { get; set; }
        public int FirstWeek { get; set; } = DefaultFirstWeek;
        public int LastWeek { get; set; } = DefaultLastWeek;
        public int TimeoutMs { get; set; } = DefaultTimeoutMs;
        public int DelayBetweenRequestsMs { get; set; } = DefaultDelayBetweenRequestsMs;
        public bool Headless { get; set; } = true;

        public WeekRange ResolveWeekRange(int? firstWeek, int? lastWeek)
        {
            var requestedFirstWeek = firstWeek ?? FirstWeek;
            var requestedLastWeek = lastWeek ?? LastWeek;

            if (requestedFirstWeek < FirstWeek || requestedLastWeek > LastWeek || requestedFirstWeek > requestedLastWeek)
            {
                throw new InvalidWeekRangeException(FirstWeek, LastWeek);
            }

            return new WeekRange(requestedFirstWeek, requestedLastWeek);
        }
    }
}
