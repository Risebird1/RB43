/*
================================================================================
DeductionReviewCheckPolicy — per AI check (dbo.fn_DeductionAiCheck key), whether
QC has taken it out of auto-approval because reviews keep upholding interviewers'
requests on it ("until it is fixed").

  ExcludeFromAutoApprove = 1  usp_AutoApprovePaymentSlaDetails skips deductions whose
                              main check is this one, so a person approves them.
  No row / 0                  the check stays automatic (default).

Written by usp_SetDeductionReviewCheckPolicy from the QC review queue. Idempotent.
================================================================================
*/
IF OBJECT_ID(N'dbo.DeductionReviewCheckPolicy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeductionReviewCheckPolicy
    (
        AiCheck                VARCHAR(30)   NOT NULL,
        ExcludeFromAutoApprove BIT           NOT NULL CONSTRAINT DF_DeductionReviewCheckPolicy_Exclude DEFAULT (0),
        ModifiedBy             VARCHAR(150)  NULL,
        ModifiedAtUtc          DATETIME      NOT NULL CONSTRAINT DF_DeductionReviewCheckPolicy_ModifiedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_DeductionReviewCheckPolicy PRIMARY KEY CLUSTERED (AiCheck ASC)
    );
END
GO
