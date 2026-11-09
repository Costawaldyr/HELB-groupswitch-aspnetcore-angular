using System.ComponentModel.DataAnnotations;
using GroupSwitch.Data.Enums;

namespace GroupSwitch.Data.Entities
{
    public class ImportRun
    {
        public const int GroupNameMaxLength = 100;

        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public ImportStatus Status { get; set; }
        public int GroupsImported { get; set; }
        public int SessionsImported { get; set; }

        [MaxLength(GroupNameMaxLength)]
        public string? RequestedGroupName { get; set; }

        [MaxLength(ScheduleCoverage.AcademicYearMaxLength)]
        public string? AcademicYear { get; set; }

        public int? FirstWeek { get; set; }
        public int? LastWeek { get; set; }
        public int SuccessfulWeeks { get; set; }
        public int RejectedWeeks { get; set; }
        public bool CancellationRequested { get; set; }

        public List<ImportLogEntry> LogEntriesList { get; set; } = new();
    }
}
