using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum NextLevelApprovalStatus
    {
        [Description("Approved")]
        Approved = 1,
        [Description("Not Approved")]
        Not_Approved = 2,
        [Description("Decision Pending")]
        Decision_Pending = 3        
    }
}
