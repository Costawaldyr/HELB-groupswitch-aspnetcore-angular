using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupSwitch.Data.Enums;

namespace GroupSwitch.Data.Entities
{
    public class ImportRun
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public ImportStatus Status { get; set; }
        public int GroupsImported { get; set; }
        public int SessionsImported { get; set; }
        public List<ImportLogEntry> LogEntriesList { get; set; } = new();
    }
}
