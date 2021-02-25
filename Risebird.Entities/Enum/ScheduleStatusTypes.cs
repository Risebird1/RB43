using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum ScheduleStatusTypes
    {
        [Description("Scheduled")]
        Scheduled = 1,
        [Description("Not Yet Scheduled")]
        NotYetScheduled = 2
    }
}
