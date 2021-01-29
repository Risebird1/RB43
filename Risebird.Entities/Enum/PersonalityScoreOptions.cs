using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum PersonalityScoreOptions
    {
        [Description("Poor")]
        Poor = 1,

        [Description("Good")]
        Good = 2,

        [Description("Excellent")]
        Excellent = 3,

        [Description("Outstanding")]
        Outstanding = 4,
    }
}
