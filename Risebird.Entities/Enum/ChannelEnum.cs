using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    class ChannelEnum
    {
    }
    public enum Prioritylevels
    {
        [Description("Low")]
        Low = 1,
        [Description("Medium")]
        Medium = 2,
        [Description("High")]
        High = 3,
        [Description("Very High")]
        VeryHigh = 4,
    }
    public enum Originlevel
    {
        [Description("New")]
        New = 0,
        [Description("Existing")]
        Existing = 1,
    }
    public enum Engagementtypelevel
    {
        [Description("Pilot")]
        Pilot = 0,
        [Description("PreConfirmed")]
        PreConfirmed = 1,
        [Description("Confirmed")]
        Confirmed = 2,
    }
    public enum ConfirmationStatuslevel
    {
        [Description("Confirmed")]
        Confirmed = 0,
        [Description("Declined by client")]
        Declinedbyclient = 1,
        [Description("Declined by Risebird")]
        DeclinedbyRisebird = 2,
        [Description("Live")]
        Live = 3,
        [Description("Hold")]
        Hold = 4,
        [Description("Completed")]
        Completed = 5,
        [Description("Archived")]
        Archived = 6,
    }
    public enum paymentstatuslevel
    {
        [Description("NDA Signed")]
        NDASigned = 0,
        [Description("T&C Signed")]
        TandCSigned = 1,
        [Description("PO Received")]
        POReceived = 2,
        [Description("Advance Payment Received")]
        AdvancePaymentReceived = 3,
    }
    public enum stagelevel
    {
        [Description("Recruiter call")]
        Recruitercall = 0,
        [Description("Delivery call")]
        Deliverycall = 1,
        [Description("SME call")]
        SMEcall = 2,
        [Description("Interviewer Approval")]
        InterviewerApproval = 3,
        [Description("Scoring Rubric Approval")]
        ScoringRubricApproval = 4,
        [Description("Questions Approval")]
        QuestionsApproval = 5,
        [Description("Candidates profile received")]
        Candidatesprofilereceived = 6,
    }
}
