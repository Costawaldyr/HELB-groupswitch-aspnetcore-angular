using GroupSwitch.Api.Dtos;
using GroupSwitch.Business.Scraping.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GroupSwitch.Tests.Scraping.Controllers
{
    public class SchedulesControllerTests
    {
        private const string SchedulesUrl = "/api/schedules";
        private const string ImportsUrl = "/api/Hyperplanning/imports";

        private ApiFactory NewFactory()
        {
            var factory = new ApiFactory();
            factory.EnsureDatabaseCreated();

            return factory;
        }

        private async Task ImportSampleDataAsync(ApiFactory factory, HttpClient client)
        {
            var started = await client.PostAsync(ImportsUrl, new StringContent(string.Empty));
            var run = await started.Content.ReadFromJsonAsync<ImportRunDto>();

            using var scope = factory.Services.CreateScope();
            var hyperplanningImportService = scope.ServiceProvider.GetRequiredService<IHyperplanningImportService>();
            await hyperplanningImportService.RunAsync(run!.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("?group=")]
        [InlineData("?group=%20")]
        public async Task GetSchedule_BlankGroup_ReturnsBadRequest(string queryString)
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync($"{SchedulesUrl}{queryString}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetSchedule_GroupNeverImported_ReturnsNotFound()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync($"{SchedulesUrl}?group=XYZ%209");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSchedule_WeeksOutsideTheSettings_ReturnsBadRequest()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();
            await ImportSampleDataAsync(factory, client);

            var response = await client.GetAsync($"{SchedulesUrl}?group=IODA%20B3&firstWeek=0");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetSchedule_AfterImport_ShowsVerifiedAndUnknownWeeks()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();
            await ImportSampleDataAsync(factory, client);

            var schedule = await client.GetFromJsonAsync<ScheduleDto>($"{SchedulesUrl}?group=IODA%20B3&firstWeek=8&lastWeek=11");

            var weeks = schedule!.Weeks.ToList();
            Assert.Equal(new[] { 8, 9, 10, 11 }, weeks.Select(week => week.Week));
            Assert.Equal(new[] { "Verified", "Unknown", "Unknown", "Verified" }, weeks.Select(week => week.Coverage));
            Assert.Equal(new[] { 9, 0, 0, 15 }, weeks.Select(week => week.Sessions.Count()));
        }

        [Fact]
        public async Task GetSchedule_SessionTimes_StayInLocalTime()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();
            await ImportSampleDataAsync(factory, client);

            var schedule = await client.GetFromJsonAsync<ScheduleDto>($"{SchedulesUrl}?group=IODA%20B3&firstWeek=11&lastWeek=11");

            var sessions = schedule!.Weeks.Single().Sessions;
            Assert.Contains(sessions, session => session.Activity == "Dév. 4 : Plateforme.net"
                && session.Start == new DateTime(2026, 11, 25, 9, 0, 0)
                && session.End == new DateTime(2026, 11, 25, 10, 0, 0));
        }
    }
}
