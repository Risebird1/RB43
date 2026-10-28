-- One code session per interview x question, for human-led interviews.
--
-- The interviewer mints a session from the interview alignment page against one of their own
-- questions (TechSkillComments), and the candidate opens it at /CodeSession/Index?token=... without
-- signing in. The token is the candidate's only credential, so it is unique, and the
-- (InterviewId, QuestionId) pair is unique so a question gets one session per interview.
--
-- QuestionText is a snapshot, not a duplicate: TechSkillComments rows are reused across every
-- interview on the channel and can be edited at any time, so a foreign key alone would let a report
-- rendered next year show a question the candidate was never asked.
--
-- Only the last run is kept (LastRunSource/LastRunOutput/LastRunStatus) plus a count, which answers
-- "did it compile" without storing a transcript of every attempt. LastRunSource means the evidence
-- survives a candidate who runs their code but never presses Submit before the call ends.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_NAME = N'InterviewCodeSessions')
BEGIN
    CREATE TABLE [dbo].[InterviewCodeSessions] (
        [Id]                       int IDENTITY(1,1) NOT NULL,
        [InterviewId]              int NOT NULL,
        [QuestionId]               int NOT NULL, -- TechSkillComments.id
        [QuestionText]             nvarchar(max) NOT NULL,
        [Token]                    uniqueidentifier NOT NULL,
        [LanguageId]               varchar(40) NULL,
        [SubmittedSource]          nvarchar(max) NULL,
        [LastRunSource]            nvarchar(max) NULL, -- the code that produced LastRunOutput, kept even if never submitted
        [LastRunOutput]            nvarchar(max) NULL, -- CodeExecutionResult.CombinedOutput
        [LastRunStatus]            varchar(40) NULL,
        [RunCount]                 int NOT NULL CONSTRAINT [DF_ICS_RunCount] DEFAULT (0),
        [CodingTimeLimitInMinutes] int NULL,       -- snapshot of the limit when the session was minted
        [CreatedBy]                varchar(200) NULL,
        [CreatedOn]                datetime NOT NULL CONSTRAINT [DF_ICS_CreatedOn] DEFAULT (GETUTCDATE()),
        [OpenedOn]                 datetime NULL,  -- first time the candidate opened the link
        [SubmittedOn]              datetime NULL,
        CONSTRAINT [PK_InterviewCodeSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_ICS_Token] UNIQUE ([Token]),
        CONSTRAINT [UQ_ICS_InterviewQuestion] UNIQUE ([InterviewId], [QuestionId])
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes
               WHERE name = N'IX_ICS_InterviewId' AND object_id = OBJECT_ID(N'dbo.InterviewCodeSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ICS_InterviewId] ON [dbo].[InterviewCodeSessions]([InterviewId])
END
GO
