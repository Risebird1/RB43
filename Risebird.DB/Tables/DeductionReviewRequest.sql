/*
================================================================================
DeductionReviewRequest — an interviewer's request to review one Payment SLA
deduction (My Quality page), and QC's single, final decision on it.

  Status          Open | Stands | Reduced | Refunded | Expired
                  (Expired = no QC reply within the deadline: the deduction stands
                  and the request is flagged to the QC lead)
  ReasonCategory  WrongStatus | Technical | CandidateCaused | ReportSavedIncorrectly | Other
  AiCheck         main check behind the deduction (dbo.fn_DeductionAiCheck), stamped
                  at request time; the upheld rate per AiCheck measures the AI
  DeductionPct    the deduction % when the review was requested
  NewDeductionPct the % after the decision (Stands = DeductionPct, Refunded = 0)
  DeductionAmount / RefundedAmount   client-rate units (% x interview rate / 100), as
                  MoneyProtectionWallet.Points
  PayoutAlreadyPaid 1 when the interviewer had already been paid for the interview
                  (InterviewPoints.Status = Paid) at decision time: the payout row is not
                  changed and Finance must settle the difference.

One request per deduction (unique InterviewId). Written by
usp_SaveDeductionReviewRequest / usp_DecideDeductionReviewRequest /
usp_ExpireDeductionReviewRequests. Idempotent.
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
