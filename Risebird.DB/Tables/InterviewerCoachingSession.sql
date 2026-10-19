/*
================================================================================
InterviewerCoachingSession — one row per coaching contact with an interviewer
(Interviewer QC View: "Log coaching session" and "Email summary").

  Channel   Call | WhatsApp | Meeting | Email | Other
  Outcome   AgreedToChange | Disputed | CallBack | NoAnswer | EmailOnly
  Themes    comma-separated coaching theme keys discussed / emailed
  CallSid   the coach's Plivo call to this interviewer in the 3 hours before
            logging, when there was one (talk time + recording attach)

Read by usp_QCActivity_CollectRows (metric Coaching), usp_GetInterviewerQCView
and usp_GetInterviewerExposure; written by usp_SaveInterviewerCoachingSession.
Idempotent. Run before those procedures.
================================================================================
*/
IF OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InterviewerCoachingSession
    (
        Id                INT            IDENTITY(1,1) NOT NULL,
        InterviewerUserId INT            NOT NULL,
        InterviewerEmail  VARCHAR(150)   NOT NULL,
        CoachEmail        VARCHAR(150)   NOT NULL,
        Channel           VARCHAR(20)    NOT NULL,
        Outcome           VARCHAR(30)    NOT NULL,
        Themes            NVARCHAR(1000) NULL,
        Agreed            NVARCHAR(2000) NULL,
        CallSid           VARCHAR(50)    NULL,
        EmailSubject      NVARCHAR(300)  NULL,
        IssueCount        INT            NULL,
        FromIst           DATE           NULL,
        ToIst             DATE           NULL,
        CreatedAtUtc      DATETIME       NOT NULL CONSTRAINT DF_InterviewerCoachingSession_CreatedAtUtc DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_InterviewerCoachingSession PRIMARY KEY CLUSTERED (Id ASC)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewerCoachingSession_Interviewer_Created'
      AND object_id = OBJECT_ID(N'dbo.InterviewerCoachingSession'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewerCoachingSession_Interviewer_Created
        ON dbo.InterviewerCoachingSession (InterviewerUserId, CreatedAtUtc DESC)
        INCLUDE (Channel, Outcome, CoachEmail);
END
GO

-- "I've read this" on the interviewer's My Quality page (usp_AcknowledgeInterviewerCoachingSession).
IF COL_LENGTH('dbo.InterviewerCoachingSession', 'AcknowledgedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.InterviewerCoachingSession ADD AcknowledgedAtUtc DATETIME NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewerCoachingSession_Coach_Created'
      AND object_id = OBJECT_ID(N'dbo.InterviewerCoachingSession'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewerCoachingSession_Coach_Created
        ON dbo.InterviewerCoachingSession (CoachEmail, CreatedAtUtc);
END
GO
