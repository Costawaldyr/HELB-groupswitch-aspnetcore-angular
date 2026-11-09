namespace GroupSwitch.Business.Scraping.Models
{
    public class UnknownGroupException : Exception
    {
        private const string MessageFormat = "Groupe inconnu : {0}.";

        public UnknownGroupException(string groupName) : base(string.Format(MessageFormat, groupName))
        {
        }
    }
}
