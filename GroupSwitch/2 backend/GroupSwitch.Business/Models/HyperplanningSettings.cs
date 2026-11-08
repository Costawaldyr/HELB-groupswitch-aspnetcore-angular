using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Models
{
    public class HyperplanningSettings
    {
        public const string SectionName = "Hyperplanning";
        public const int DefaultTimeoutMs = 30000;

        public string PageUrl { get; set; } = string.Empty;
        public DateTime FirstMonday { get; set; }
        public int TimeoutMs { get; set; } = DefaultTimeoutMs;
        public bool Headless { get; set; } = true;
    }
}
