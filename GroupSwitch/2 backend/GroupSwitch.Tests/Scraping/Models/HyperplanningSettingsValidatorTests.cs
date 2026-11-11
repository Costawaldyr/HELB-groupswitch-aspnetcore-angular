using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Tests.Scraping.Models
{
    public class HyperplanningSettingsValidatorTests
    {
        private readonly HyperplanningSettingsValidator _validator = new();

        private HyperplanningSettings ValidSettings()
        {
            return new HyperplanningSettings
            {
                PageUrl = "https://planning.helb-prigogine.be/2026-2027/invite",
                AcademicYear = "2026-2027",
                FirstMonday = new DateTime(2026, 9, 14)
            };
        }

        [Fact]
        public void Validate_ValidSettings_Succeeds()
        {
            var result = _validator.Validate(null, ValidSettings());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Validate_FirstMondayIsNotAMonday_Fails()
        {
            var settings = ValidSettings();
            settings.FirstMonday = new DateTime(2026, 9, 15);

            var result = _validator.Validate(null, settings);

            Assert.True(result.Failed);
        }

        [Fact]
        public void Validate_LastWeekBeforeFirstWeek_Fails()
        {
            var settings = ValidSettings();
            settings.FirstWeek = 10;
            settings.LastWeek = 5;

            var result = _validator.Validate(null, settings);

            Assert.True(result.Failed);
        }

        [Fact]
        public void Validate_EmptyPageUrl_Fails()
        {
            var settings = ValidSettings();
            settings.PageUrl = string.Empty;

            var result = _validator.Validate(null, settings);

            Assert.True(result.Failed);
        }

        [Fact]
        public void Validate_TwoProblems_ReportsBoth()
        {
            var settings = ValidSettings();
            settings.PageUrl = string.Empty;
            settings.TimeoutMs = 0;

            var result = _validator.Validate(null, settings);

            Assert.NotNull(result.Failures);
            Assert.Equal(2, result.Failures.Count());
        }
    }
}
