using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;
using GroupSwitch.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GroupSwitch.Business
{
    public static class BusinessServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddSingleton(provider =>
                new SessionTimeCalculator(provider.GetRequiredService<IOptions<HyperplanningSettings>>().Value.FirstMonday));
            services.AddSingleton<ISessionBuilder, SessionBuilder>();
            services.AddSingleton<IGroupListParser, GroupListParser>();
            services.AddSingleton<IWeekScheduleParser, WeekScheduleParser>();
            services.AddSingleton<IWeekResponseConverter, WeekResponseConverter>();
            services.AddSingleton<IGroupScheduleCollector, GroupScheduleCollector>();
            services.AddSingleton<IImportQueue, ImportQueue>();

            services.AddScoped<IHyperplanningScraper, PlaywrightHyperplanningScraper>();
            services.AddScoped<IImportJournal, ImportJournal>();
            services.AddScoped<IImportService, ImportService>();

            return services;
        }
    }
}