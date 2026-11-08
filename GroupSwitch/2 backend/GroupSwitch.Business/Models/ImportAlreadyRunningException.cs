using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Models
{
    public class ImportAlreadyRunningException : Exception
    {
        private const string DefaultMessage = "Un import est déjà en cours.";

        public ImportAlreadyRunningException() : base(DefaultMessage)
        {
        }
    }
}
