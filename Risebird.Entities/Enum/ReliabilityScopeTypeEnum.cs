using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum ReliabilityScopeTypeEnum
    {
        RedoInterviewInitiated=1,
        CustomerRating4OrLess=2,
        CustomerRating5O=3,
        VideoConfirmationNo=4,
        VideoConfirmation_Yes=5,
        LateLoginForTakingInterviewBy5OrMoreMinutes=6,
        SuspiciousInterviewer=7,
        PerRedFlagByQCTeam=8,
        PerPaymentSLA=9,
        NoRedFlagsByQCTeam=10,
        UnassignmentInterviewPenalty=11,
        LateInterviewReportPenalty=12,
        InterviewerNoShow=13,
        SelectionRateLessThan20PercentageOrMoreThan40Percentage=14,
        SelectionRateWithin20PercentageTo40Percentage=15,
        LessThan5QuestionsAddedInTheReport=16,
        FiveOrMoreQuestionAddedInTheReport=17,
        LessThan250CharactersCommentForRejectedInterview=18,
        GreaterThan250CharactersCommentOrRejectedInterview=19,
        LessThan500CharactersCommentForSelectedInterview=20,
        GreaterThan500CharactersCommentOrSelectedInterview21

    }
}
