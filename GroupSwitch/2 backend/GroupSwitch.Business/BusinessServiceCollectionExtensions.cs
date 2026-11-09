using GroupSwitch.Business.Scraping.Conversion;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Parsing;
using GroupSwitch.Business.Scraping.Retrieval;
using GroupSwitch.Business.Scraping.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GroupSwitch.Business
{
    public static class BusinessServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Hyperplanning settings
            services.AddSingleton(provider =>
                new SessionTimeCalculator(provider.GetRequiredService<IOptions<HyperplanningSettings>>().Value.FirstMonday));

            // Scraping dependencies
            services.AddSingleton<ISessionBuilder, SessionBuilder>();
            services.AddSingleton<IGroupListParser, GroupListParser>();
            services.AddSingleton<IWeekScheduleParser, WeekScheduleParser>();
            services.AddSingleton<IWeekAssessmentService, WeekAssessmentService>();
            services.AddSingleton<IGroupScheduleCollectorService, GroupScheduleCollectorService>();
            services.AddSingleton<IImportQueueService, ImportQueueService>();
            services.AddSingleton<IValidateOptions<HyperplanningSettings>, HyperplanningSettingsValidator>();

            // Scraping services
            services.AddScoped<IHyperplanningScraper, PlaywrightHyperplanningScraper>();
            services.AddScoped<IImportLogService, ImportLogService>();
            services.AddScoped<IHyperplanningImportService, HyperplanningImportService>();


            return services;
        }
    }
}
