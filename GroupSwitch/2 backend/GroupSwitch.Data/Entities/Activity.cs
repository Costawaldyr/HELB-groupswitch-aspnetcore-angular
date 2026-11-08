using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Data.Entities
{
    [Index(nameof(Title), IsUnique = true)]
    public class Activity
    {
        public int Id { get; set; }

        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public List<ScheduledSession> SessionList { get; set; } = new();

    }
}
