using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum ClientRoleEnum
    {
        [Description("HRs")]
        IRFUser = 1,

        [Description("Head HR")]
        IRFOwner = 2,

        [Description("Staffing Agency")]
        RestrictedIRFowner = 4,

        [Description("Business Owner")]
        MultipleIRFowner = 6,
    }
}
