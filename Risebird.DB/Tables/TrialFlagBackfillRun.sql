/*
================================================================================
TrialFlagBackfillRun — one row per "Backfill trial flags" run on Trial Flag Review
(DashboardController.BackfillTrialFlags), with who ran it, so QC Activity can count
the run under Trial (metric TrialBackfill), as IRF Audit does with
IrfAuditRiskBackfillRun.

  ExecutedBy   session email of the person who clicked Backfill
  InterviewId  the single interview asked for, or NULL for a batch
  Inserted / Skipped / Failed   the run's result
Times are UTC. Idempotent: IF NOT EXISTS. Also ensured at runtime by
TrialJudgmentRepository.RecordBackfillRunAsync.
================================================================================
*/
IF OBJECT_ID(N'dbo.TrialFlagBackfillRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrialFlagBackfillRun
    (
        Id            INT IDENTITY(1,1) NOT NULL,
        ExecutedAtUtc DATETIME      NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_ExecutedAtUtc DEFAULT (GETUTCDATE()),
        ExecutedBy    VARCHAR(150)  NOT NULL,
        InterviewId   INT           NULL,
        Inserted      INT           NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_Inserted DEFAULT (0),
        Skipped       INT           NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_Skipped DEFAULT (0),
        Failed        INT           NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_Failed DEFAULT (0),
        Message       VARCHAR(500)  NULL,
        CONSTRAINT PK_TrialFlagBackfillRun PRIMARY KEY CLUSTERED (Id ASC)
    );

    CREATE NONCLUSTERED INDEX IX_TrialFlagBackfillRun_ExecutedBy_At
        ON dbo.TrialFlagBackfillRun (ExecutedBy, ExecutedAtUtc);
END
GO
