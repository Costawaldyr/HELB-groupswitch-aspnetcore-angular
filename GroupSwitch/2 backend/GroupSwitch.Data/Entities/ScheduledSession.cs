using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Data.Entities
{
    [Index(nameof(StudentGroupId), nameof(Start))]
    public class ScheduledSession
    {
        public const int RoomMaxLength = 500;
        public const int TeacherMaxLength = 500;
        public const int RemarkMaxLength = 500;

        public int Id { get; set; }

        public int WeekNumber { get; set; }

        [MaxLength(ScheduleCoverage.AcademicYearMaxLength)]
        public string? AcademicYear { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        [MaxLength(RoomMaxLength)]
        public string Room { get; set; } = string.Empty;

        [MaxLength(TeacherMaxLength)]
        public string Teacher { get; set; } = string.Empty;

        [MaxLength(RemarkMaxLength)]
        public string Remark { get; set; } = string.Empty;

        public bool IsCancelled { get; set; }

        public StudentGroup? StudentGroup { get; set; }
        public int StudentGroupId { get; set; }

        public Activity? Activity { get; set; }
        public int ActivityId { get; set; }
    }
}
