/*
================================================================================
InterviewerActionLog — one row per status change a portal user made on an
interviewer (append-only). Times are UTC.

  ActionType  SuspiciousUndone | Blocked | Unblocked | AccessRevoked |
              AccessRestored | Inactivated | Reactivated | PaymentHold |
              PaymentHoldReleased
  Reason      hold reason, or inactivation type plus comment
  ActedBy     session email of the user who made the change
  Source      procedure that wrote the row

Written by usp_undoSuspiciousInterviewer, usp_UpdateInterviewerFlags,
usp_InactivateInterviewer, usp_ReactivateInterviewer and
usp_AddOrEditInterviewerProfile (each skips logging while this table is
missing). Read by usp_QCActivity_CollectRows and usp_QCActivity_Diagnostics.
Marking an interviewer suspicious is not logged here: SuspiciousInterviewer
already keeps one row per mark.

Idempotent. Run before the procedures that write or read it.
================================================================================
*/
IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InterviewerActionLog
    (
        Id                INT            IDENTITY(1,1) NOT NULL,
        InterviewerUserId INT            NULL,
        InterviewerEmail  VARCHAR(200)   NULL,
        ActionType        VARCHAR(40)    NOT NULL,
        Reason            NVARCHAR(500)  NULL,
        ActedBy           VARCHAR(200)   NULL,
        ActedAtUtc        DATETIME       NOT NULL CONSTRAINT DF_InterviewerActionLog_ActedAtUtc DEFAULT (GETUTCDATE()),
        Source            VARCHAR(80)    NULL,
        CONSTRAINT PK_InterviewerActionLog PRIMARY KEY CLUSTERED (Id ASC)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewerActionLog_ActedBy_ActedAt'
      AND object_id = OBJECT_ID(N'dbo.InterviewerActionLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewerActionLog_ActedBy_ActedAt
        ON dbo.InterviewerActionLog (ActedBy, ActedAtUtc);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewerActionLog_Interviewer_ActedAt'
      AND object_id = OBJECT_ID(N'dbo.InterviewerActionLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewerActionLog_Interviewer_ActedAt
        ON dbo.InterviewerActionLog (InterviewerUserId, ActedAtUtc);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewerActionLog_ActedAt'
      AND object_id = OBJECT_ID(N'dbo.InterviewerActionLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewerActionLog_ActedAt
        ON dbo.InterviewerActionLog (ActedAtUtc)
        INCLUDE (ActionType, ActedBy);
END
GO
