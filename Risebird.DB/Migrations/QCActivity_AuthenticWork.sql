/*
================================================================================
QC Activity: authentic QC work — schema (parts 1 and 2). Idempotent; safe to re-run.

  1. PaymentSLADetailsHistory.IsApproved   (fix 7: approvals counted per save)
  2. dbo.TrialFlagBackfillRun              (fix 4: who ran the trial-flag backfill)
  3. dbo.QCActivitySetting + seed rows     (part 2 thresholds; the QC lead tunes them)

Deploy in this order (merge into the Azure DevOps copy; do not copy over it):

Part 1 fixes
  Migrations/QCActivity_AuthenticWork.sql            (this file)
  Trigger/tr_PaymentSLADetails.sql                   (copies IsApproved into history)
  StoredProcedures/usp_FetchRibbonDetailsValues.sql  (Suspicious list returns InterviewerUserId)
  StoredProcedures/usp_undoSuspiciousInterviewer.sql (returns RowsChanged)
  StoredProcedures/usp_InactivateInterviewer.sql     (IST CreatedOn; returns RowsChanged)
  StoredProcedures/usp_ReactivateInterviewer.sql     (IST CreatedOn)
  StoredProcedures/usp_UpdateRedMarkeredInterviewerstatus.sql (IST; returns RowsChanged)
  StoredProcedures/usp_SaveorUpdateProxyReport.sql   (logs ProxyForm)
  StoredProcedures/usp_AddRedoInterviewDetails.sql   (logs RedoRequested)

Part 2 data
  StoredProcedures/usp_GetInterviewerRiskOverview.sql   (@IdsOnly mode)
  StoredProcedures/usp_QCActivity_CollectRows.sql
  StoredProcedures/usp_QCActivity_ComputeFlags.sql
  StoredProcedures/usp_QCActivity_HighRisk.sql
  StoredProcedures/usp_GetQCActivityDashboardSummary.sql
  StoredProcedures/usp_GetQCActivityDashboardDetails.sql
  StoredProcedures/usp_GetQCActivityFlags.sql
  StoredProcedures/usp_GetQCActivityCoverage.sql

Then the web app.
================================================================================
*/
IF OBJECT_ID(N'dbo.PaymentSLADetailsHistory', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PaymentSLADetailsHistory', 'IsApproved') IS NULL
    ALTER TABLE dbo.PaymentSLADetailsHistory ADD IsApproved INT NULL;
GO

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

IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QCActivitySetting
    (
        Name          VARCHAR(50)    NOT NULL,
        Value         NVARCHAR(1000) NOT NULL,
        Description   NVARCHAR(300)  NULL,
        ModifiedBy    VARCHAR(150)   NULL,
        ModifiedAtUtc DATETIME       NOT NULL CONSTRAINT DF_QCActivitySetting_ModifiedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_QCActivitySetting PRIMARY KEY CLUSTERED (Name ASC)
    );
END
GO

;WITH Seed (Name, Value, Description) AS (
    SELECT 'MinCallSeconds',   N'120', N'Connected coaching call: answered, to an interviewer, at least this many seconds'
    UNION ALL SELECT 'MinCommentChars', N'20', N'Authentic action: comment at least this many characters'
    UNION ALL SELECT 'StockComments', N'ok,okay,done,fine,checked,check,na,n/a,nil,none,reviewed,good,noted,yes,no,-,.', N'Comments that do not count as a real comment (comma-separated, case-insensitive)'
    UNION ALL SELECT 'BurstCount', N'10', N'Burst flag: this many actions of the same type by one person ...'
    UNION ALL SELECT 'BurstMinutes', N'10', N'... within this many minutes'
    UNION ALL SELECT 'ReversalDays', N'7', N'Reversed by another QC: undone by a different person within this many days'
    UNION ALL SELECT 'ShortCallSeconds', N'30', N'Short calls flag: dialled calls under this many seconds ...'
    UNION ALL SELECT 'ShortCallPct', N'50', N'... are more than this % of the person''s dialled calls'
)
INSERT INTO dbo.QCActivitySetting (Name, Value, Description)
SELECT s.Name, s.Value, s.Description
FROM Seed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.QCActivitySetting x WHERE x.Name = s.Name);
GO
