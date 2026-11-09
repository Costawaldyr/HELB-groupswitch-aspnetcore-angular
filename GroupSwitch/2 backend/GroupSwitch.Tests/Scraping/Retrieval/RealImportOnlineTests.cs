using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Parsing;
using GroupSwitch.Business.Scraping.Retrieval;
using GroupSwitch.Business.Scraping.Services;
using GroupSwitch.Data.Enums;
using GroupSwitch.Data.Repositories;
using GroupSwitch.Tests.Scraping.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace GroupSwitch.Tests.Scraping.Retrieval
{
    [Trait("Category", "Online")]
    public class RealImportOnlineTests
    {
        private const string GroupName = "IODA B3";
        private const int FirstWeek = 1;
        private const int LastWeek = 14;

        private readonly ITestOutputHelper _output;
        private readonly IOptions<HyperplanningSettings> _settings = Options.Create(new HyperplanningSettings
        {
            PageUrl = "https://planning.helb-prigogine.be/2026-2027/invite",
            AcademicYear = "2026-2027",
            FirstMonday = TestObjects.FirstMonday,
            FirstWeek = FirstWeek,
            LastWeek = LastWeek,
            Headless = true
        });

        public RealImportOnlineTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private HyperplanningImportService NewImportService(SqliteTestDatabase database)
        {
            var importRunRepository = new ImportRunRepository(database._dbContext);

            return new HyperplanningImportService(
                new PlaywrightHyperplanningScraper(new GroupListParser(), _settings),
                TestObjects.NewWeekAssessmentService(),
                new ScheduleRepository(database._dbContext),
                importRunRepository,
                new ImportLogService(importRunRepository),
                _settings);
        }

        private async Task<GroupScheduleOverview> ImportAndDescribeAsync(SqliteTestDatabase database, string label)
        {
            var importService = NewImportService(database);
            var run = await importService.StartAsync(new ImportRequest(GroupName, FirstWeek, LastWeek));

            await importService.RunAsync(run.Id);

            var savedRun = await database._dbContext.ImportRuns
                .Include(importRun => importRun.LogEntriesList)
                .AsNoTracking()
                .SingleAsync(importRun => importRun.Id == run.Id);
            var overview = await new ScheduleQueryService(new ScheduleRepository(database._dbContext), _settings)
                .GetScheduleAsync(GroupName, FirstWeek, LastWeek);
            var sessions = overview.Weeks.SelectMany(week => week.Sessions).ToList();

            _output.WriteLine($"== {label} : run {savedRun.Id} | {savedRun.Status} | weeks ok={savedRun.SuccessfulWeeks} rejected={savedRun.RejectedWeeks} | groups={savedRun.GroupsImported} sessions={savedRun.SessionsImported}");

            foreach (var entry in savedRun.LogEntriesList.OrderBy(entry => entry.Id))
            {
                _output.WriteLine($"   log {entry.Level}: {entry.Message}");
            }

            foreach (var week in overview.Weeks)
            {
                _output.WriteLine($"   week {week.Week,2} | {week.Coverage,-13} | sessions={week.Sessions.Count,2} | cancelled={week.Sessions.Count(session => session.IsCancelled)}");
            }

            if (sessions.Any())
            {
                var activityCount = sessions.Select(session => session.ActivityId).Distinct().Count();
                _output.WriteLine($"   total sessions={sessions.Count} cancelled={sessions.Count(session => session.IsCancelled)} activities={activityCount} first={sessions.Min(session => session.Start):yyyy-MM-dd HH:mm} last={sessions.Max(session => session.End):yyyy-MM-dd HH:mm}");
            }

            Assert.Equal(ImportStatus.Finished, savedRun.Status);
            Assert.Equal(LastWeek - FirstWeek + 1, savedRun.SuccessfulWeeks);
            Assert.Equal(0, savedRun.RejectedWeeks);

            return overview;
        }

        [Fact]
        public async Task Import_IodaB3AllWeeks_CoversEveryWeekAndIsRepeatable()
        {
            using var database = new SqliteTestDatabase();

            var first = await ImportAndDescribeAsync(database, "first import");
            var second = await ImportAndDescribeAsync(database, "second import");

            var weekSeven = first.Weeks.Single(week => week.Week == 7);
            Assert.Equal(WeekCoverageStatus.VerifiedEmpty, weekSeven.Coverage);
            Assert.Empty(weekSeven.Sessions);
            Assert.DoesNotContain(first.Weeks, week => week.Coverage == WeekCoverageStatus.Unknown);
            Assert.Equal(first.Weeks.Select(week => week.Coverage), second.Weeks.Select(week => week.Coverage));
            Assert.Equal(first.Weeks.Select(week => week.Sessions.Count), second.Weeks.Select(week => week.Sessions.Count));
        }
    }
}
