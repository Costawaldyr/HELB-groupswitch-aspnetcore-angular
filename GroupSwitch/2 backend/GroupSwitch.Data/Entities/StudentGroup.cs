using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace GroupSwitch.Data.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    public class StudentGroup
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public List<ScheduledSession> SessionList { get; set; } = new();
    }
}
