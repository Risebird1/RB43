using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum TrainingStatus
    {
        [Description("Completed")]
        Completed = 1,
        [Description("Not Completed")]
        NotCompleted = 2
    }
}
