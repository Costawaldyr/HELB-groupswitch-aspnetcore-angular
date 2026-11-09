using System.Text.Json;
using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Parsing
{
    public class ScheduleRequestParser
    {
        private static readonly char[] DomainBrackets = { '[', ']' };

        public ScheduleRequest? Parse(string? requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(requestBody);

                var data = document.RootElement.GetProperty("donneesSec");
                var searchedGroups = data.GetProperty("_Signature_").GetProperty("listeRecherche");
                var groupName = searchedGroups[0].GetProperty("L").GetString();
                var domain = data.GetProperty("donnees").GetProperty("Domaine").GetProperty("V").GetString();

                if (groupName is null)
                {
                    return null;
                }

                return new ScheduleRequest(groupName, WeekOf(domain));
            }
            catch (Exception exception) when (IsUnexpectedShape(exception))
            {
                return null;
            }
        }

        private int? WeekOf(string? domain)
        {
            var text = domain?.Trim(DomainBrackets);

            return int.TryParse(text, out var week) ? week : null;
        }

        private bool IsUnexpectedShape(Exception exception)
        {
            return exception is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException;
        }
    }
}
