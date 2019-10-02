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
        Dropped = 7,
        [Description("Waste- Filled by Internal Resource Pool")]
        WasteFilledbyInternalResourcePool = 5,
        [Description("Waste- Filled by Referral ")]
        WasteFilledbyReferral = 6,
        [Description("Waste- Decision maker Indecisive ")]
        WasteDecisionmakerIndecisive = 7,
        [Description("Waste- Requirement Dropped")]
        WasteRequirementDropped = 8,
        [Description("Waste- Requirement Shrank")]
        WasteRequirementShrank = 9,
        [Description("Waste- Requirement Scope Changed ")]
        WasteRequirementScopeChanged = 10,
        [Description("Waste- Requirement Onhold")]
        WasteRequirementOnhold = 11,
        [Description("Waste- Next step not available")]
        WasteNextstepnotavailable = 12,
        [Description("Waste- Filled by competitor")]
        WasteFilledbycompetitor = 13,
        [Description("Waste- Candidate No-show")]
        WasteCandidateNoshow = 14,
        [Description("Waste- Candidate Indecisive")]
        WasteCandidateIndecisive = 15,
        [Description("Waste- Candidate Attitude Misfit")]
        WasteCandidateAttitudeMisfit = 16,
        [Description("Waste- Candidate Not-Interested")]
        WasteCandidateNotInterested = 17,
        [Description("Waste- Candidate Better offer")]
        WasteCandidateBetteroffer = 18,
        [Description("Waste- Candidate Not reachable")]
        WasteCandidateNotreachable = 19
    }
}
