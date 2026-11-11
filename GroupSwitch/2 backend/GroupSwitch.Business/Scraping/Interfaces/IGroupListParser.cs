

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IGroupListParser
    {
        IEnumerable<string> ParseNames(string json);
    }
}
