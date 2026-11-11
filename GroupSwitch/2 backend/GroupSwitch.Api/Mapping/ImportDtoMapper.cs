using GroupSwitch.Api.Dtos;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Api.Mapping
{
    public class ImportDtoMapper : IImportDtoMapper
    {
        public ImportRunDto ToDto(ImportRun run)
        {
            return new ImportRunDto(
                run.Id,
                run.StartedAt,
                run.FinishedAt,
                run.Status.ToString(),
                run.GroupsImported,
                run.SessionsImported,
                run.RequestedGroupName,
                run.AcademicYear,
                run.FirstWeek,
                run.LastWeek,
                run.SuccessfulWeeks,
                run.RejectedWeeks,
                run.CancellationRequested);
        }

        public ImportDetailsDto ToDetailsDto(ImportRun run)
        {
            var log = run.LogEntriesList
                .OrderBy(entry => entry.Id)
                .Select(entry => new ImportLogDto(entry.Moment, entry.Level, entry.Message));

            return new ImportDetailsDto(ToDto(run), log);
        }
    }
}
