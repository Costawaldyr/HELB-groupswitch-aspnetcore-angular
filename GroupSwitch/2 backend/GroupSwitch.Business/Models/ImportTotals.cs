namespace GroupSwitch.Business.Models
{
    public class ImportTotals
    {
        public int Groups { get; private set; }
        public int Sessions { get; private set; }

        public void AddGroup(int sessionCount)
        {
            Groups++;
            Sessions += sessionCount;
        }
    }
}