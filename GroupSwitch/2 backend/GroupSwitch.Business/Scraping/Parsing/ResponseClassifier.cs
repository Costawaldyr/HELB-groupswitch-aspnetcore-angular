using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Scraping.Parsing
{
    public static class HyperplanningFunctions
    {
        public const string GroupList = "FonctionRenvoyerListeDeRessource";
        public const string Schedule = "FonctionEmploiDuTemps";
    }

    public class ResponseClassifier
    {
        public string FunctionNameOf(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);

                if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("nom", out var name))
                {
                    return name.GetString() ?? string.Empty;
                }

                return string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        public bool IsGroupList(string body)
        {
            return FunctionNameOf(body) == HyperplanningFunctions.GroupList;
        }

        public bool IsSchedule(string body)
        {
            return FunctionNameOf(body) == HyperplanningFunctions.Schedule;
        }
    }
}
