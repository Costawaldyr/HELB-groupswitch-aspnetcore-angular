using GroupSwitch.Api.Dtos;
using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Scraping;
using GroupSwitch.Tests.Scraping;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GroupSwitch.Tests.Controllers
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

            var queue = factory.Services.GetRequiredService<IImportQueue>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var request = await queue.DequeueAsync(timeout.Token);

            Assert.Equal(run!.Id, request.ImportRunId);
            Assert.Null(request.GroupName);
        }

        [Fact]
        public async Task ShouldQueueRequestedGroup()
        {
            using var factory = NewFactory();
            var client = factory.CreateClient();

            await client.PostAsync($"{ImportsUrl}?group=IODA%20B3", EmptyBody());

            var queue = factory.Services.GetRequiredService<IImportQueue>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var request = await queue.DequeueAsync(timeout.Token);

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
                var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
                await importService.RunAsync(run!.Id, name => true, QuadrimesterFilters.IsInFirstQuadrimester);
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
    }
}