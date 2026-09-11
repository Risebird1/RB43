-- ============================================================================
-- Per-question code sessions.
--
-- One row per ATTEMPT. The spec captures per attempt, not per question: a question can
-- carry several sessions (a fumbled attempt, a lost link, a second run at the same
-- problem), and generating a new one never replaces or invalidates an earlier one.
-- AttemptNumber orders them within (InterviewId, question).
--
-- Stdout and Stderr are kept SEPARATELY on purpose - a client should be able to see that
-- something printed and something else threw.
--
-- RunState carries three values, not two:
--   1 Executed      ran to completion, whatever the exit code
--   2 Failed        ran, and the code did not compile or threw - this is evidence
--   3 NotExecuted   the runner was unavailable, or the code changed after the last run
-- Failed and NotExecuted must never look alike in a report: one says something true about
-- the candidate, the other says nothing about them at all.
--
-- SessionState:
--   1 Generated  2 Opened  3 InProgress  4 Submitted  5 Expired
-- Only Submitted reaches a client report.
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InterviewCodeSession]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[InterviewCodeSession](
    [id]                    [int] IDENTITY(1,1) NOT NULL,

    -- Scope. IRFId is the integer IRF.id, carried so the report can group without a join.
    [InterviewId]           [int] NOT NULL,
    [IRFId]                 [int] NULL,

    -- The question. NULL SampleQuestionId means an ad-hoc question typed during the
    -- interview and not saved back to the interviewer's bank.
    [SampleQuestionId]      [int] NULL,
    [SkillId]               [int] NULL,
    [SkillName]             [nvarchar](200) NULL,

    -- A SNAPSHOT of the question as asked. The bank entry can be edited later; the report
    -- must show what was actually put to this candidate.
    [QuestionText]          [nvarchar](max) NULL,

    -- The unauthenticated link. Scoped to exactly one session and nothing else.
    [SessionToken]          [varchar](64) NOT NULL,

    [AttemptNumber]         [int] NOT NULL CONSTRAINT [DF_InterviewCodeSession_AttemptNumber] DEFAULT (1),
    [SessionState]          [int] NOT NULL CONSTRAINT [DF_InterviewCodeSession_SessionState] DEFAULT (1),

    [Language]              [varchar](50) NULL,
    -- The version the runner actually used, which is not always the version requested.
    [LanguageVersionUsed]   [varchar](50) NULL,

    [SubmittedCode]         [nvarchar](max) NULL,
    [Stdout]                [nvarchar](max) NULL,
    [Stderr]                [nvarchar](max) NULL,
    [ExitStatus]            [int] NULL,
    [RunState]              [int] NULL,
    [RunDurationMs]         [int] NULL,

    -- Stored, not published. Surfacing it to a client is a separate decision.
    [PasteEvents]           [int] NOT NULL CONSTRAINT [DF_InterviewCodeSession_PasteEvents] DEFAULT (0),

    [TimeLimitMinutes]      [int] NULL,
    [CreatedBy]             [varchar](200) NULL,
    [CreatedAt]             [datetime] NULL,
    [OpenedAt]              [datetime] NULL,
    [LastSavedAt]           [datetime] NULL,
    [SubmittedAt]           [datetime] NULL,
    [ExpiresAt]             [datetime] NULL,

 CONSTRAINT [PK_InterviewCodeSession] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- The candidate link resolves by token on every request, so it carries a unique index.
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_InterviewCodeSession_SessionToken'
               AND object_id = OBJECT_ID(N'[dbo].[InterviewCodeSession]'))
BEGIN
    CREATE UNIQUE INDEX [UX_InterviewCodeSession_SessionToken]
        ON [dbo].[InterviewCodeSession]([SessionToken])
END
GO

-- The interviewer lists sessions for an interview, and the report reads submitted ones.
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_InterviewCodeSession_Interview'
               AND object_id = OBJECT_ID(N'[dbo].[InterviewCodeSession]'))
BEGIN
    CREATE INDEX [IX_InterviewCodeSession_Interview]
        ON [dbo].[InterviewCodeSession]([InterviewId], [SampleQuestionId], [AttemptNumber])
END
GO
