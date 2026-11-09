namespace GroupSwitch.Api.Dtos
{
    public record ImportRunDto(
        int Id,
        DateTime StartedAt,
        DateTime? FinishedAt,
        string Status,
        int GroupsImported,
        int SessionsImported,
        string? RequestedGroupName,
        string? AcademicYear,
        int? FirstWeek,
        int? LastWeek,
        int SuccessfulWeeks,
        int RejectedWeeks,
        bool CancellationRequested
    );
}
