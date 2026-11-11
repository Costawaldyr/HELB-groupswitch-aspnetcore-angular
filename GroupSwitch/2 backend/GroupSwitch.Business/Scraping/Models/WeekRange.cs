namespace GroupSwitch.Business.Scraping.Models
{
    public record WeekRange(int FirstWeek, int LastWeek)
    {
        public IEnumerable<int> Weeks()
        {
            for (var week = FirstWeek; week <= LastWeek; week++)
            {
                yield return week;
            }
        }
    }
}
