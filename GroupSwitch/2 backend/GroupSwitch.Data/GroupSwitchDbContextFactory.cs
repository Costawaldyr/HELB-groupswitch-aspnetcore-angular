using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GroupSwitch.Data
{
    public class GroupSwitchDbContextFactory : IDesignTimeDbContextFactory<GroupSwitchDbContext>
    {
        public GroupSwitchDbContext CreateDbContext(string[]? args = null)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<GroupSwitchDbContext>();
            optionsBuilder.UseSqlServer(configuration[DatabaseSettings.ConnectionStringKey]);

            return new GroupSwitchDbContext(optionsBuilder.Options);
        }
    }
}
