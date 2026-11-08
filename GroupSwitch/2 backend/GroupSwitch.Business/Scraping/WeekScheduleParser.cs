using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using System.Text.Json;


namespace GroupSwitch.Business.Scraping
{
    public class WeekScheduleParser : IWeekScheduleParser
    {
        public IEnumerable<ParsedSession> Parse(string json)
        {
            using var document = JsonDocument.Parse(json);

            var data = document.RootElement.GetProperty("donneesSec").GetProperty("donnees");

            foreach (var course in ItemsOf(data, "ListeCours"))
            {
                yield return ToSession(course, course.GetProperty("listeC"), false);
            }

            foreach (var cancelled in ItemsOf(data, "ListeAnnulationsCours"))
            {
                yield return ToSession(cancelled, cancelled.GetProperty("listeContenus"), true);
            }
        }

        private IEnumerable<JsonElement> ItemsOf(JsonElement parent, string name)
        {
            if (parent.TryGetProperty(name, out var items))
            {
                return items.EnumerateArray();
            }

            return Enumerable.Empty<JsonElement>();
        }

        private ParsedSession ToSession(JsonElement course, JsonElement contents, bool isCancelled)
        {
            return new ParsedSession(
                course.GetProperty("G").GetInt32(),
                course.GetProperty("p").GetInt32(),
                course.GetProperty("d").GetInt32(),
                NamesOf(contents, HyperplanningKinds.Subject).DefaultIfEmpty(string.Empty).First(),
                string.Join(HyperplanningKinds.Separator, NamesOf(contents, HyperplanningKinds.Teacher)),
                string.Join(HyperplanningKinds.Separator, NamesOf(contents, HyperplanningKinds.Room)),
                RemarkOf(course, contents, isCancelled),
                isCancelled
            );
        }

        private IEnumerable<string> NamesOf(JsonElement contents, int kind)
        {
            var wanted = contents.EnumerateArray().Where(content => content.GetProperty("G").GetInt32() == kind);

            foreach (var content in wanted)
            {
                var value = content.GetProperty("C");

                if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in value.EnumerateArray())
                    {
                        yield return item.GetProperty("L").GetString() ?? string.Empty;
                    }
                }
                else if (value.TryGetProperty("L", out var label))
                {
                    yield return label.GetString() ?? string.Empty;
                }
            }
        }

        private string RemarkOf(JsonElement course, JsonElement contents, bool isCancelled)
        {
            if (isCancelled && course.TryGetProperty("commentaire", out var comment))
            {
                return comment.GetString() ?? string.Empty;
            }

            var remarks = contents.EnumerateArray()
                .Where(content => content.GetProperty("G").GetInt32() == HyperplanningKinds.Remark)
                .Select(content => content.GetProperty("C"))
                .Where(value => value.TryGetProperty("str", out _))
                .Select(value => value.GetProperty("str").GetString() ?? string.Empty);

            return remarks.DefaultIfEmpty(string.Empty).First();
        }
    }
}
