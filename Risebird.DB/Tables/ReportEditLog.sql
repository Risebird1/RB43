/*
================================================================================
ReportEditLog — one row per changed field on a submitted interview report
(append-only). Times are UTC.

  FieldName   today always InterviewerComment
  OldValue /  full text before and after the edit, stored HTML-encoded as the
  NewValue    app saves it
  EditedBy    session email of the editor
  Source      procedure that wrote the row

Written by usp_UpdateReportDetails (skips logging while this table is missing).
Read by usp_QCActivity_CollectRows (metric ReportEdited; Id is the dashboard's
"View change" RefId), usp_GetReportEdit and usp_QCActivity_Diagnostics.

Idempotent. Run before the procedures that write or read it.
================================================================================
*/
IF OBJECT_ID(N'dbo.ReportEditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportEditLog
    (
        Id           INT            IDENTITY(1,1) NOT NULL,
        InterviewId  INT            NOT NULL,
        FieldName    VARCHAR(60)    NOT NULL,
        OldValue     NVARCHAR(MAX)  NULL,
        NewValue     NVARCHAR(MAX)  NULL,
        EditedBy     VARCHAR(200)   NULL,
        EditedAtUtc  DATETIME       NOT NULL CONSTRAINT DF_ReportEditLog_EditedAtUtc DEFAULT (GETUTCDATE()),
        Source       VARCHAR(80)    NULL,
        CONSTRAINT PK_ReportEditLog PRIMARY KEY CLUSTERED (Id ASC)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ReportEditLog_EditedBy_EditedAt'
      AND object_id = OBJECT_ID(N'dbo.ReportEditLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReportEditLog_EditedBy_EditedAt
        ON dbo.ReportEditLog (EditedBy, EditedAtUtc);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ReportEditLog_Interview_EditedAt'
      AND object_id = OBJECT_ID(N'dbo.ReportEditLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReportEditLog_Interview_EditedAt
        ON dbo.ReportEditLog (InterviewId, EditedAtUtc);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ReportEditLog_EditedAt'
      AND object_id = OBJECT_ID(N'dbo.ReportEditLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReportEditLog_EditedAt
        ON dbo.ReportEditLog (EditedAtUtc);
END
GO
