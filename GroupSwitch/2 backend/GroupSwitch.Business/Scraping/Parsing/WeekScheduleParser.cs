using System.Text.Json;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Parsing
{
    public class WeekScheduleParser : IWeekScheduleParser
    {
        private const string InvalidPropertyFormat = "Propriété absente ou invalide : {0}.";

        public IEnumerable<ParsedSession> Parse(string json)
        {
            using var document = JsonDocument.Parse(json);

            var secondaryData = Required(document.RootElement, "donneesSec", JsonValueKind.Object);
            var data = Required(secondaryData, "donnees", JsonValueKind.Object);

            foreach (var course in Required(data, "ListeCours", JsonValueKind.Array).EnumerateArray())
            {
                yield return ToSession(course, "listeC", false);
            }

            foreach (var cancelled in OptionalArray(data, "ListeAnnulationsCours"))
            {
                yield return ToSession(cancelled, "listeContenus", true);
            }
        }

        private ParsedSession ToSession(JsonElement course, string contentsName, bool isCancelled)
        {
            var contents = Required(course, contentsName, JsonValueKind.Array).EnumerateArray().ToList();

            return new ParsedSession(
                RequiredInt(course, "G"),
                RequiredInt(course, "p"),
                RequiredInt(course, "d"),
                NamesOf(contents, HyperplanningKinds.Subject).DefaultIfEmpty(string.Empty).First(),
                string.Join(HyperplanningKinds.Separator, NamesOf(contents, HyperplanningKinds.Teacher)),
                string.Join(HyperplanningKinds.Separator, NamesOf(contents, HyperplanningKinds.Room)),
                RemarkOf(course, contents, isCancelled),
                isCancelled
            );
        }

        private IEnumerable<string> NamesOf(List<JsonElement> contents, int kind)
        {
            var wanted = contents.Where(content => KindOf(content) == kind);

            foreach (var content in wanted)
            {
                foreach (var label in LabelsOf(content))
                {
                    yield return label;
                }
            }
        }

        private IEnumerable<string> LabelsOf(JsonElement content)
        {
            if (!content.TryGetProperty("C", out var value))
            {
                yield break;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    if (TryGet(item, "L", JsonValueKind.String, out var itemLabel))
                    {
                        yield return itemLabel.GetString() ?? string.Empty;
                    }
                }
            }
            else if (TryGet(value, "L", JsonValueKind.String, out var label))
            {
                yield return label.GetString() ?? string.Empty;
            }
        }

        private string RemarkOf(JsonElement course, List<JsonElement> contents, bool isCancelled)
        {
            if (isCancelled && TryGet(course, "commentaire", JsonValueKind.String, out var comment))
            {
                return comment.GetString() ?? string.Empty;
            }

            return RemarksOf(contents).DefaultIfEmpty(string.Empty).First();
        }

        private IEnumerable<string> RemarksOf(List<JsonElement> contents)
        {
            var remarks = contents.Where(content => KindOf(content) == HyperplanningKinds.Remark);

            foreach (var content in remarks)
            {
                if (content.TryGetProperty("C", out var value) && TryGet(value, "str", JsonValueKind.String, out var text))
                {
                    yield return text.GetString() ?? string.Empty;
                }
            }
        }

        private int? KindOf(JsonElement content)
        {
            return TryGet(content, "G", JsonValueKind.Number, out var kind) && kind.TryGetInt32(out var number) ? number : null;
        }

        private IEnumerable<JsonElement> OptionalArray(JsonElement parent, string name)
        {
            if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value))
            {
                if (value.ValueKind != JsonValueKind.Array)
                {
                    throw new HyperplanningFormatException(string.Format(InvalidPropertyFormat, name));
                }

                return value.EnumerateArray();
            }

            return Enumerable.Empty<JsonElement>();
        }

        private int RequiredInt(JsonElement parent, string name)
        {
            var value = Required(parent, name, JsonValueKind.Number);

            if (!value.TryGetInt32(out var number))
            {
                throw new HyperplanningFormatException(string.Format(InvalidPropertyFormat, name));
            }

            return number;
        }

        private JsonElement Required(JsonElement parent, string name, JsonValueKind kind)
        {
            if (!TryGet(parent, name, kind, out var value))
            {
                throw new HyperplanningFormatException(string.Format(InvalidPropertyFormat, name));
            }

            return value;
        }

        private bool TryGet(JsonElement parent, string name, JsonValueKind kind, out JsonElement value)
        {
            value = default;

            return parent.ValueKind == JsonValueKind.Object
                && parent.TryGetProperty(name, out value)
                && value.ValueKind == kind;
        }
    }
}
