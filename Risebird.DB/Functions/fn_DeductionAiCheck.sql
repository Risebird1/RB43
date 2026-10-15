IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_DeductionAiCheck]')
                      AND type IN (N'IF', N'TF', N'FN'))
BEGIN
    DROP FUNCTION dbo.fn_DeductionAiCheck
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- The main AI check behind a Payment SLA deduction, as a stable key. Single source of truth for:
--   * DeductionReviewRequest.AiCheck (stamped when an interviewer requests a review)
--   * the upheld rate per check on the QC review queue
--   * usp_AutoApprovePaymentSlaDetails, which skips checks QC took out of auto-approval
--     (DeductionReviewCheckPolicy.ExcludeFromAutoApprove).
--
-- A reason column counts when it holds positive evidence (same rule as
-- TrialFlagEvidenceBadgeHelper.IsPositiveEvidence: not blank / 0 / no / n / false / na / n/a / none / null;
-- "Video Off When Mandatory" is compliant checklist text, not an issue). The first matching
-- column in the order below is the main check. Keys and labels must match DeductionReviewConstants.AiChecks in C#.
CREATE FUNCTION dbo.fn_DeductionAiCheck (@InterviewId INT)
RETURNS VARCHAR(30)
AS
BEGIN
    DECLARE @Check VARCHAR(30);

    SELECT TOP 1 @Check =
        CASE
            WHEN dbo.fn_DeductionReasonIsIssue(p.AttendanceJoinCompliance) = 1 THEN 'LateJoin'
            WHEN dbo.fn_DeductionReasonIsIssue(p.PoorFeedbackComments) = 1 THEN 'ShortComment'
            WHEN dbo.fn_DeductionReasonIsIssue(p.TimeManagement) = 1 THEN 'Duration'
            WHEN dbo.fn_DeductionReasonIsIssue(p.QuestionsRelevance) = 1 THEN 'Questions'
            WHEN dbo.fn_DeductionReasonIsIssue(p.Inaccuratedecisionmaking) = 1 THEN 'Decision'
            WHEN dbo.fn_DeductionReasonIsIssue(p.ScoreRubricIgnorance) = 1 THEN 'Rubric'
            WHEN dbo.fn_DeductionReasonIsIssue(p.WrongStatusUpdates) = 1 THEN 'StatusUpdate'
            WHEN dbo.fn_DeductionReasonIsIssue(p.InterviewerVideo) = 1 THEN 'CameraOff'
            WHEN dbo.fn_DeductionReasonIsIssue(p.ProctoringIgnorance) = 1 THEN 'Proctoring'
            WHEN dbo.fn_DeductionReasonIsIssue(p.CandidateInteractionQuality) = 1 THEN 'CandidateInteraction'
            WHEN dbo.fn_DeductionReasonIsIssue(p.PoorCommunicationSkills) = 1 THEN 'Communication'
            WHEN dbo.fn_DeductionReasonIsIssue(p.WeakInfrastructure) = 1 THEN 'Infrastructure'
            WHEN dbo.fn_DeductionReasonIsIssue(p.LimitedHandoncoding) = 1 THEN 'HandsOn'
            WHEN dbo.fn_DeductionReasonIsIssue(p.UnethicalPractices) = 1 THEN 'Conduct'
            ELSE 'Other'
        END
    FROM dbo.PaymentSLADetails p
    WHERE p.Interviewid = @InterviewId
    ORDER BY ISNULL(p.PaymentDeduction, 0) DESC, p.id DESC;

    RETURN ISNULL(@Check, 'Other');
END
GO
