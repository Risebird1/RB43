using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum InterviewStatus
    {
        [Description("Proxy")]
        Proxy = 1,
        [Description("Selected")]
        Selected = 2,
        [Description("Rejected")]
        Rejected = 3,
        [Description("Border Line")]
        BorderLine = 4

    }

    public enum ReceiveMeetingStatus
    {
        [Description("Confirmed")]
        Confirmed = 1,
        [Description("Cancelled")]
        Cancelled = 2
    }

    public enum InterviewSlots
    {
        [Description("Morning")]
        Morning = 1,
        [Description("Evening")]
        Evening = 2
    }

    public enum MeetingStatus
    {
        [Display(Name = "No Show(Candidate)")]
        [Description("No Show(Candidate)")]
        NoShow = 1,
        [Description("Scheduled")]
        Scheduled = 2,
        [Description("Dropped")]
        Dropped = 3,
        [Description("Completed")]
        Completed = 4,

        [Display(Name = "No Show(Interviewer)")]
        [Description("No Show(Interviewer)")]
        NoShowInterviewer = 5,

    }

    public enum OfferStatus
    {
        [Description("None")]
        None = -1,
        [Description("Offer Released")]
        OfferReleased = 1,
        [Description("Offer Accepted")]
        OfferAccepted = 2,
        [Description("Offer Declined")]
        OfferDenied = 3,
        [Description("Not offered")]
        Notoffered = 4,
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

    public enum JoiningStatus
    {
        [Description("None")]
        None = -1,
        [Description("Joined")]
        Joining = 1,
        [Description("Not Joined")]
        NotJoining = 2,
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
    public enum FinalInterviewMeetingStatus
    {
        [Description("No Show")]
        NoShow = 1,
        [Description("Selected")]
        Selected = 2,
        [Description("Completed")]
        Completed = 3,
        [Description("Dropped")]
        Dropped = 4

    }

    public enum PreparationStatus
    {
        [Description("Completed")]
        Completed = 1,
        [Description("Not Completed")]
        NotCompleted = 2

    }
    public enum L2InterviewStatus
    {
        [Description("Selected")]
        Selected = 1,
        [Description("Rejected")]
        Rejected = 2
    }

    public enum ClientInterviewStatus
    {

        [Description("Offered")]
        Offered = 1,
        [Description("Not Offered")]
        NotOffered = 2
    }

    public enum CompanyType
    {

        [Description("Product")]
        Product = 1,
        [Description("Service")]
        Service = 2
    }

    public enum JobLevels
    {

        [Description("SE")]
        SE = 1,
        [Description("SSE")]
        SSE = 2,
        [Description("Lead")]
        Lead = 3,
        [Description("Manager")]
        Manager = 4,
        [Description("Architect")]
        Architect = 5       
    }

    public enum QuestionsLevel
    {

        [Description("Poor")]
        Poor = 1,
        [Description("Average")]
        Average = 2,
        [Description("Above Average")]
        AboveAverage = 3,
        [Description("Good")]
        Good = 4,
        [Description("Excellent")]
        Excellent = 5,
        [Description("Outstanding")]
        Outstanding = 6
    }

    public enum ConfirmationStatus
    {

        [Description("Selected")]
        Selected = 1,
        [Description("Rejected")]
        Rejected = 2

    }
    public enum InterviewerApproved
    {
        [Description("Not Approved")]
        NotApproved = 1,
        [Description("Approved")]
        Approved = 2,
    }
    public enum PaymentStatus
    {
        [Description("Pending")]
        Pending = 0,
        [Description("Paid")]
        Paid = 1
    }
    //public enum JoinabilityType
    //{

    //    [Description("Job Location")]
    //    JobLocation = 1,
    //    [Description("Approved Per Hour Rate")]
    //    ApprovedPerHourRate = 2,
    //    [Description("Approved Visa Type")]
    //    ApprovedVisaType = 3,
    //    [Description("Max Notice period (Days)")]
    //    MaxNoticeperiodDays = 4,

    //}

    //public enum SelectabilityType
    //{

    //    [Description("Technology Stack")]
    //    TechnologyStack = 1,
    //    [Description("Career Progression")]
    //    CareerProgression = 2,
    //    [Description("Industry")]
    //    Industry = 3,
    //    [Description("Domain")]
    //    Domain = 4,
    //    [Description("Project")]
    //    Project = 5,
    //    [Description("Previous Employers")]
    //    PreviousEmployers = 6,
    //    [Description("Academics")]
    //    Academics = 7,
    //}

    //public enum SkillGeneralType
    //{

    //    [Description("Logical")]
    //    Logical = 1,
    //    [Description("Analytical")]
    //    Analytical = 2,
    //    [Description("Communication")]
    //    Communication = 3
    //}
    public enum InstaReceiveMeetingStatus
    {
        [Description("Processing")]
        Processing = 0,
        [Description("Confirmed")]
        Confirmed = 1
    }
}
