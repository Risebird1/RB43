using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum BooleanResultsEnum
    {
        [Description("True")]
        True = 1,

        [Description("False")]
        False = 2,

        [Description("Failed")]
        Failed = 3
    }
}
