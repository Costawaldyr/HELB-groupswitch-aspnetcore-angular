using GroupSwitch.Data.Interfaces;
using GroupSwitch.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroupSwitch.Data
{
    public static class DataServiceCollectionExtensions
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<GroupSwitchDbContext>(options =>
                options.UseSqlServer(configuration[DatabaseSettings.ConnectionStringKey]));

            services.AddScoped<IScheduleRepository, ScheduleRepository>();
            services.AddScoped<IImportRunRepository, ImportRunRepository>();

            return services;
        }
    }
}