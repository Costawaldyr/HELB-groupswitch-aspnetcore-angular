

namespace GroupSwitch.Business.Interfaces
{
    public interface IGroupListParser
    {
        IEnumerable<string> ParseNames(string json);
    }
}
