using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GroupSwitch.Data.Entities
{
    [Index(nameof(StudentGroupId), nameof(AcademicYear), nameof(WeekNumber), IsUnique = true)]
    public class ScheduleCoverage
    {
        public const int AcademicYearMaxLength = 20;

        public int Id { get; set; }

        public StudentGroup? StudentGroup { get; set; }
        public int StudentGroupId { get; set; }

        [MaxLength(AcademicYearMaxLength)]
        public string AcademicYear { get; set; } = string.Empty;

        public int WeekNumber { get; set; }
        public DateTime LastVerifiedAtUtc { get; set; }
        public int SessionCount { get; set; }
        public int LastImportRunId { get; set; }
    }
}
