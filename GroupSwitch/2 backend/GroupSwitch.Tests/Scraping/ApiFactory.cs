using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Data;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GroupSwitch.Tests.Scraping
{
    public class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<GroupSwitchDbContext>>();
                services.AddDbContext<GroupSwitchDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IHostedService>();

                services.RemoveAll<IHyperplanningScraper>();
                services.AddScoped<IHyperplanningScraper>(_ => new FakeHyperplanningScraper(SampleData.TwoGroupsResponses()));
            });
        }

        public void EnsureDatabaseCreated()
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<GroupSwitchDbContext>().Database.EnsureCreated();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection.Dispose();
        }
    }
}