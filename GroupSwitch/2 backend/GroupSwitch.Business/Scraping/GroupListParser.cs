using GroupSwitch.Business.Interfaces;
using System.Text.Json;

namespace GroupSwitch.Business.Scraping
{
    public class GroupListParser : IGroupListParser
    {
        public IEnumerable<string> ParseNames(string json)
        {
            using var document = JsonDocument.Parse(json);

            var groups = document.RootElement
                .GetProperty("donneesSec")
                .GetProperty("donnees")
                .GetProperty("ListeRessources")
                .GetProperty("Liste");

            foreach (var group in groups.EnumerateArray())
            {
                yield return group.GetProperty("L").GetString() ?? string.Empty;
            }
        }
    }
}
