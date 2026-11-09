namespace GroupSwitch.Business.Scraping.Models
{
    public class InvalidWeekRangeException : Exception
    {
        private const string MessageFormat = "Les semaines doivent respecter {0} <= firstWeek <= lastWeek <= {1}.";

        public InvalidWeekRangeException(int firstAllowedWeek, int lastAllowedWeek)
            : base(string.Format(MessageFormat, firstAllowedWeek, lastAllowedWeek))
        {
        }
    }
}
