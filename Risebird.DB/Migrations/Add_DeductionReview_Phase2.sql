/*
================================================================================
Deduction review (My Quality phase 2) — schema. Idempotent; safe to re-run.

  1. dbo.DeductionReviewRequest      review requests and QC's final decision
  2. dbo.DeductionReviewCheckPolicy  AI checks taken out of auto-approval

Deploy in this order (after Add_InterviewerMyQuality_Phase1.sql):
  Migrations/Add_DeductionReview_Phase2.sql            (this file)
  Functions/fn_DeductionReasonIsIssue.sql
  Functions/fn_DeductionAiCheck.sql
  StoredProcedures/usp_SaveDeductionReviewRequest.sql
  StoredProcedures/usp_DecideDeductionReviewRequest.sql
  StoredProcedures/usp_ExpireDeductionReviewRequests.sql   (also creates usp_MarkDeductionReviewLeadNotified)
  StoredProcedures/usp_GetDeductionReviewQueue.sql         (also creates usp_SetDeductionReviewCheckPolicy)
  StoredProcedures/usp_GetInterviewerMyQuality.sql
  StoredProcedures/usp_GetInterviewerRiskOverview.sql
  StoredProcedures/usp_AutoApprovePaymentSlaDetails.sql
  StoredProcedures/usp_QCActivity_CollectRows.sql
  StoredProcedures/usp_GetQCActivityDashboardSummary.sql
  StoredProcedures/usp_QCActivity_Diagnostics.sql
Then the web app and the timer functions (DeductionReviewExpiry).

Rollback: redeploy the previous usp_AutoApprovePaymentSlaDetails first (it reads the
policy table). The other objects are only read by the new pages.
================================================================================
*/
IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeductionReviewRequest
    (
        Id                  INT            IDENTITY(1,1) NOT NULL,
        InterviewId         INT            NOT NULL,
        InterviewerUserId   INT            NOT NULL,
        InterviewerEmail    VARCHAR(150)   NULL,
        IrfId               INT            NULL,
        DeductionPct        INT            NOT NULL,
        DeductionAmount     DECIMAL(18,2)  NULL,
        ReasonCategory      VARCHAR(30)    NOT NULL,
        Note                NVARCHAR(500)  NOT NULL,
        AiCheck             VARCHAR(30)    NOT NULL,
        RequestedAtUtc      DATETIME       NOT NULL CONSTRAINT DF_DeductionReviewRequest_RequestedAtUtc DEFAULT (GETUTCDATE()),
        DueAtUtc            DATETIME       NOT NULL,
        Status              VARCHAR(20)    NOT NULL CONSTRAINT DF_DeductionReviewRequest_Status DEFAULT ('Open'),
        NewDeductionPct     INT            NULL,
        RefundedAmount      DECIMAL(18,2)  NULL,
        DecidedBy           VARCHAR(150)   NULL,
        DecidedAtUtc        DATETIME       NULL,
        DecisionNote        NVARCHAR(500)  NULL,
        LeadNotifiedAtUtc   DATETIME       NULL,
        PayoutAlreadyPaid   BIT            NOT NULL CONSTRAINT DF_DeductionReviewRequest_PayoutAlreadyPaid DEFAULT (0),
        CONSTRAINT PK_DeductionReviewRequest PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_DeductionReviewRequest_Interview UNIQUE (InterviewId),
        CONSTRAINT CK_DeductionReviewRequest_Status CHECK (Status IN ('Open', 'Stands', 'Reduced', 'Refunded', 'Expired'))
    );
END
GO

IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.DeductionReviewRequest', 'PayoutAlreadyPaid') IS NULL
    ALTER TABLE dbo.DeductionReviewRequest
        ADD PayoutAlreadyPaid BIT NOT NULL CONSTRAINT DF_DeductionReviewRequest_PayoutAlreadyPaid DEFAULT (0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DeductionReviewRequest_Status_Requested'
               AND object_id = OBJECT_ID(N'dbo.DeductionReviewRequest'))
    CREATE NONCLUSTERED INDEX IX_DeductionReviewRequest_Status_Requested
        ON dbo.DeductionReviewRequest (Status, RequestedAtUtc) INCLUDE (InterviewId, AiCheck, DueAtUtc);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DeductionReviewRequest_User_Requested'
               AND object_id = OBJECT_ID(N'dbo.DeductionReviewRequest'))
    CREATE NONCLUSTERED INDEX IX_DeductionReviewRequest_User_Requested
        ON dbo.DeductionReviewRequest (InterviewerUserId, RequestedAtUtc);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DeductionReviewRequest_Decided'
               AND object_id = OBJECT_ID(N'dbo.DeductionReviewRequest'))
    CREATE NONCLUSTERED INDEX IX_DeductionReviewRequest_Decided
        ON dbo.DeductionReviewRequest (DecidedBy, DecidedAtUtc) INCLUDE (Status);
GO

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
