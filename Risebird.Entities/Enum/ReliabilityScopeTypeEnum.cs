using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum ReliabilityScopeTypeEnum
    {
        [Description("Redo Interview Initiated")]
        RedoInterviewInitiated=1,
        [Description("Customer rating - 4 or less")]
        CustomerRating4OrLess=2,
        [Description("Customer Rating - 5")]
        CustomerRating5O =3,
        [Description("Video Confirmation - No")]
        VideoConfirmationNo =4,
        [Description("Video Confirmation - Yes")]
        VideoConfirmationYes =5,
        [Description("Late login for taking Interview by 5 or more minutes")]
        LateLoginForTakingInterviewBy5OrMoreMinutes =6,
        [Description("Suspicious Interviewer")]
        SuspiciousInterviewer =7,
        [Description("Per Red Flag by QC Team")]
        PerRedFlagByQCTeam =8,
        [Description("Per Payment SLA")]
        PerPaymentSLA =9,
        [Description("No Red Flags by QC Team")]
        NoRedFlagsByQCTeam =10,
        [Description("Unassignment Interview Penalty")]
        UnassignmentInterviewPenalty =11,
        [Description("Late Interview Report Penalty")]
        LateInterviewReportPenalty =12,
        [Description("Interviewer No-show")]
        InterviewerNoShow =13,
        [Description("Selection Rate less than 20% or more than 40%")]
        SelectionRateLessThan20PercentageOrMoreThan40Percentage =14,
        [Description("Selection Rate within 20% to 40%")]
        SelectionRateWithin20PercentageTo40Percentage =15,
        [Description("Less than 5 Questions added in the Report")]
        LessThan5QuestionsAddedInTheReport =16,
        [Description("5 or more Question Added in the Report")]
        FiveOrMoreQuestionAddedInTheReport =17,
        [Description("Less than 250 Characters Comment for Rejected Interview")]
        LessThan250CharactersCommentForRejectedInterview =18,
        [Description("Greater than 250 Characters Comment for Rejected Interview")]
        GreaterThan250CharactersCommentOrRejectedInterview =19,
        [Description("Less than 500 Characters Comment for Selected Interview")]
        LessThan500CharactersCommentForSelectedInterview =20,
        [Description("Greater than 500 Characters Comment for Selected Interview")]
        GreaterThan500CharactersCommentOrSelectedInterview =21
    }
}
