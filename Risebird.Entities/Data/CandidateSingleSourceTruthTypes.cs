using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public static class CandidateSingleSourceTruthTypes
    {
        public static readonly string Completed = "Completed";
        public static readonly string NotScheduled = "Not Scheduled";
        public static readonly string FutureScheduled = "Future Scheduled";
        public static readonly string Reschedule = "Reschedule";
        public static readonly string Pending = "Pending";
        public static readonly string Cold = "Cold";
    }
}
