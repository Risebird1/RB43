/*
================================================================================
Interviewer "My Quality" page — phase 1 schema (idempotent; safe to re-run).

  1. dbo.InterviewerFeedbackText            new table (wording per theme key)
  2. InterviewerCoachingSession.AcknowledgedAtUtc   new nullable column

Then deploy:
  StoredProcedures/usp_GetInterviewerMyQuality.sql
  StoredProcedures/usp_AcknowledgeInterviewerCoachingSession.sql

Rollback: the page only reads; dropping the procedures disables it. The column and
table hold no data the rest of the portal depends on.
================================================================================
*/
IF OBJECT_ID(N'dbo.InterviewerFeedbackText', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InterviewerFeedbackText
    (
        ThemeKey   VARCHAR(50)    NOT NULL,
        Title      NVARCHAR(200)  NOT NULL,
        WhatToDo   NVARCHAR(500)  NOT NULL,
        IsActive   BIT            NOT NULL CONSTRAINT DF_InterviewerFeedbackText_IsActive DEFAULT (1),
        ModifiedBy VARCHAR(150)   NULL,
        ModifiedAt DATETIME       NOT NULL CONSTRAINT DF_InterviewerFeedbackText_ModifiedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_InterviewerFeedbackText PRIMARY KEY CLUSTERED (ThemeKey ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.InterviewerCoachingSession', 'AcknowledgedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.InterviewerCoachingSession ADD AcknowledgedAtUtc DATETIME NULL;
END
GO
