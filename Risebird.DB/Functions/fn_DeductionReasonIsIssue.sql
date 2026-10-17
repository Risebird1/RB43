IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_DeductionReasonIsIssue]')
                      AND type IN (N'IF', N'TF', N'FN'))
BEGIN
    DROP FUNCTION dbo.fn_DeductionReasonIsIssue
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- 1 when a PaymentSLADetails reason column holds positive evidence of an issue.
-- Same rule as TrialFlagEvidenceBadgeHelper.IsPositiveEvidence, plus the compliant checklist text
-- "Video Off When Mandatory" (InterviewerCoachingBriefBuilder.CompliantReasonTokens), which is not an issue.
-- Used by fn_DeductionAiCheck. Deploy before it.
CREATE FUNCTION dbo.fn_DeductionReasonIsIssue (@Value VARCHAR(250))
RETURNS BIT
AS
BEGIN
    DECLARE @v VARCHAR(250) = LOWER(LTRIM(RTRIM(ISNULL(@Value, ''))));
    IF @v IN ('', '0', 'no', 'n', 'false', 'na', 'n/a', 'none', 'null', 'video off when mandatory')
        RETURN 0;
    RETURN 1;
END
GO
