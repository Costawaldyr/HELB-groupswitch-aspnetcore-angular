using GroupSwitch.Api.Dtos;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GroupSwitch.Tests.Scraping.Controllers
{
    public class HyperplanningControllerTests
    {
        private const string ImportsUrl = "/api/Hyperplanning/imports";

        private ApiFactory NewFactory()
        {
            var factory = new ApiFactory();
            factory.EnsureDatabaseCreated();

            return factory;
        }

        private StringContent EmptyBody()
        {
            return new StringContent(string.Empty);
        }

        [Fact]
        public async Task ShouldReturnAcceptedWithRunAndLocation()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync(ImportsUrl, EmptyBody());

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var run = await response.Content.ReadFromJsonAsync<ImportRunDto>();
            Assert.Equal("Running", run!.Status);
            Assert.Contains($"{ImportsUrl}/{run.Id}", response.Headers.Location!.ToString());
        }

        [Fact]
        public async Task ShouldReturnConflictWhenAnotherImportIsRunning()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            await client.PostAsync(ImportsUrl, EmptyBody());
            var second = await client.PostAsync(ImportsUrl, EmptyBody());

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task ShouldReturnNotFoundForUnknownId()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync($"{ImportsUrl}/999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostImport_QueuesARequestForAllGroups()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync(ImportsUrl, EmptyBody());
            var run = await response.Content.ReadFromJsonAsync<ImportRunDto>();

            var importQueueService = factory.Services.GetRequiredService<IImportQueueService>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var request = await importQueueService.DequeueAsync(timeout.Token);

            Assert.Equal(run!.Id, request.ImportRunId);
            Assert.Null(request.GroupName);
        }

        [Fact]
        public async Task ShouldQueueRequestedGroup()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            await client.PostAsync($"{ImportsUrl}?group=IODA%20B3", EmptyBody());

            var importQueueService = factory.Services.GetRequiredService<IImportQueueService>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var request = await importQueueService.DequeueAsync(timeout.Token);

            Assert.Equal("IODA B3", request.GroupName);
        }

        [Fact]
        public async Task GetImport_AfterTheRun_ReturnsFinishedRunWithLog()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();
            var response = await client.PostAsync(ImportsUrl, EmptyBody());
            var run = await response.Content.ReadFromJsonAsync<ImportRunDto>();

            using (var scope = factory.Services.CreateScope())
            {
                var hyperplanningImportService = scope.ServiceProvider.GetRequiredService<IHyperplanningImportService>();
                await hyperplanningImportService.RunAsync(run!.Id);
            }

            var details = await client.GetFromJsonAsync<ImportDetailsDto>($"{ImportsUrl}/{run.Id}");

            Assert.Equal("Finished", details!.Run.Status);
            Assert.Equal(2, details.Run.GroupsImported);
            Assert.Equal(39, details.Run.SessionsImported);
            Assert.Equal(4, details.Log.Count());
        }

        [Theory]
        [InlineData("?group")]
        [InlineData("?group=")]
        [InlineData("?group=%20")]
        [InlineData("?group=%20%20%20")]
        [InlineData("?group=%09")]
        public async Task ShouldReturnBadRequestAndStartNothingForBlankGroup(string queryString)
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync($"{ImportsUrl}{queryString}", EmptyBody());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var latest = await client.GetFromJsonAsync<List<ImportRunDto>>(ImportsUrl);
            Assert.NotNull(latest);
            Assert.Empty(latest);
        }

        private async Task RunImportAsync(ApiFactory factory, int importRunId)
        {
            using var scope = factory.Services.CreateScope();
            var hyperplanningImportService = scope.ServiceProvider.GetRequiredService<IHyperplanningImportService>();

            await hyperplanningImportService.RunAsync(importRunId);
        }

        [Fact]
        public async Task PostImport_UnknownGroup_ReturnsBadRequestAndStartsNothing()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync($"{ImportsUrl}?group=XYZ%209", EmptyBody());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var latest = await client.GetFromJsonAsync<List<ImportRunDto>>(ImportsUrl);
            Assert.NotNull(latest);
            Assert.Empty(latest);
        }

        [Theory]
        [InlineData("?firstWeek=0")]
        [InlineData("?lastWeek=15")]
        [InlineData("?firstWeek=9&lastWeek=8")]
        public async Task PostImport_WeeksOutsideTheSettings_ReturnsBadRequest(string queryString)
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync($"{ImportsUrl}{queryString}", EmptyBody());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostImport_GroupAndWeeks_ReturnsTheRequestedScope()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync($"{ImportsUrl}?group=IODA%20B3&firstWeek=8&lastWeek=11", EmptyBody());
            var run = await response.Content.ReadFromJsonAsync<ImportRunDto>();

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("IODA B3", run!.RequestedGroupName);
            Assert.Equal(8, run.FirstWeek);
            Assert.Equal(11, run.LastWeek);
        }

        [Fact]
        public async Task CancelImport_UnknownId_ReturnsNotFound()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync($"{ImportsUrl}/999/cancel", EmptyBody());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CancelImport_RunningImport_ReturnsAcceptedAndRecordsTheRequest()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();
            var started = await client.PostAsync(ImportsUrl, EmptyBody());
            var run = await started.Content.ReadFromJsonAsync<ImportRunDto>();

            var response = await client.PostAsync($"{ImportsUrl}/{run!.Id}/cancel", EmptyBody());
            var details = await client.GetFromJsonAsync<ImportDetailsDto>($"{ImportsUrl}/{run.Id}");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.True(details!.Run.CancellationRequested);
        }

        [Fact]
        public async Task CancelImport_FinishedImport_ReturnsConflict()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();
            var started = await client.PostAsync(ImportsUrl, EmptyBody());
            var run = await started.Content.ReadFromJsonAsync<ImportRunDto>();
            await RunImportAsync(factory, run!.Id);

            var response = await client.PostAsync($"{ImportsUrl}/{run.Id}/cancel", EmptyBody());

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetImport_AfterTheRun_ShowsSuccessfulAndRejectedWeeks()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();
            var started = await client.PostAsync(ImportsUrl, EmptyBody());
            var run = await started.Content.ReadFromJsonAsync<ImportRunDto>();
            await RunImportAsync(factory, run!.Id);

            var details = await client.GetFromJsonAsync<ImportDetailsDto>($"{ImportsUrl}/{run.Id}");

            Assert.Equal(3, details!.Run.SuccessfulWeeks);
            Assert.Equal(0, details.Run.RejectedWeeks);
        }

        [Fact]
        public async Task GetGroups_ReturnsTheHyperplanningGroupNames()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            var groupNames = await client.GetFromJsonAsync<List<string>>("/api/Hyperplanning/groups");

            Assert.Equal(new[] { "IODA B3", "SI 3" }, groupNames);
        }
    }
}