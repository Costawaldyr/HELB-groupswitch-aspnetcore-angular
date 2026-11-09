namespace GroupSwitch.Business.Scraping.Models
{
    public class ImportTotals
    {
        private readonly List<GroupImportSummary> _groupSummaries = new();

        public int Groups => _groupSummaries.Count;
        public int Sessions => _groupSummaries.Sum(summary => summary.SessionCount);
        public int SuccessfulWeeks { get; private set; }
        public int RejectedWeeks { get; private set; }

        public IEnumerable<GroupImportSummary> GroupSummaries => _groupSummaries;

        public void AddSavedWeek(string groupName, int sessionCount)
        {
            var summary = _groupSummaries.FirstOrDefault(existing => existing.GroupName == groupName);

            if (summary is null)
            {
                summary = new GroupImportSummary(groupName);
                _groupSummaries.Add(summary);
            }

            summary.AddWeek(sessionCount);
            SuccessfulWeeks++;
        }

        public void AddRejectedWeek()
        {
            RejectedWeeks++;
        }
    }
}
