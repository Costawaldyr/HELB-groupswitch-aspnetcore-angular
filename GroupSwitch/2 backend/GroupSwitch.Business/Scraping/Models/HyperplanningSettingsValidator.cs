using Microsoft.Extensions.Options;

namespace GroupSwitch.Business.Scraping.Models
{
    public class HyperplanningSettingsValidator : IValidateOptions<HyperplanningSettings>
    {
        public const int LastPossibleWeek = 52;

        private const string PageUrlMessage = "Hyperplanning:PageUrl doit être une adresse https absolue.";
        private const string AcademicYearMessage = "Hyperplanning:AcademicYear est obligatoire.";
        private const string FirstMondayMessage = "Hyperplanning:FirstMonday doit être un lundi.";
        private const string WeekRangeMessage = "Hyperplanning:FirstWeek et LastWeek doivent respecter 1 <= FirstWeek <= LastWeek <= 52.";
        private const string TimeoutMessage = "Hyperplanning:TimeoutMs doit être supérieur à 0.";
        private const string DelayMessage = "Hyperplanning:DelayBetweenRequestsMs ne peut pas être négatif.";

        public ValidateOptionsResult Validate(string? name, HyperplanningSettings settings)
        {
            var errors = ErrorsOf(settings).ToList();

            return errors.Any() ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
        }

        private IEnumerable<string> ErrorsOf(HyperplanningSettings settings)
        {
            if (!Uri.TryCreate(settings.PageUrl, UriKind.Absolute, out var pageUri) || pageUri.Scheme != Uri.UriSchemeHttps)
            {
                yield return PageUrlMessage;
            }

            if (string.IsNullOrWhiteSpace(settings.AcademicYear))
            {
                yield return AcademicYearMessage;
            }

            if (settings.FirstMonday.DayOfWeek != DayOfWeek.Monday)
            {
                yield return FirstMondayMessage;
            }

            if (settings.FirstWeek < 1 || settings.LastWeek < settings.FirstWeek || settings.LastWeek > LastPossibleWeek)
            {
                yield return WeekRangeMessage;
            }

            if (settings.TimeoutMs <= 0)
            {
                yield return TimeoutMessage;
            }

            if (settings.DelayBetweenRequestsMs < 0)
            {
                yield return DelayMessage;
            }
        }
    }
}
