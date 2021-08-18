using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class CalendlySchedulingLinkRequestBodyData
    {
        public int max_event_count { get; set; }
        public string owner { get; set; }
        public string owner_type { get; set; }
    }
}
