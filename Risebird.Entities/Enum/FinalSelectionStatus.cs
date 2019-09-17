using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum FinalSelectionStatus
    {
        [Description("Selected")]
        Approved = 1,
        [Description("Rejected")]
        Not_Approved = 2,
        [Description("Interview Pending")]
        Interview_Pending = 3,
        [Description("Decision Pending")]
        Decision_Pending = 4,
        [Description("No Show")]
        No_Show = 5,
        [Description("Candidate Backedout")]
        Candidate_Backedout = 6,
        [Description("Dropped")]
        Dropped = 7
    }
}
