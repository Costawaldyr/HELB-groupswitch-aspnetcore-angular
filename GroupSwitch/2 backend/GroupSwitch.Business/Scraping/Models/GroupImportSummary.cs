namespace GroupSwitch.Business.Scraping.Models
{
    public class GroupImportSummary
    {
        public GroupImportSummary(string groupName)
        {
            GroupName = groupName;
        }

        public string GroupName { get; }
        public int SessionCount { get; private set; }
        public int WeekCount { get; private set; }

        public void AddWeek(int sessionCount)
        {
            WeekCount++;
            SessionCount += sessionCount;
        }
    }
}
