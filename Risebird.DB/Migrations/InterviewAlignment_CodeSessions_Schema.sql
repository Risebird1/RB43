-- Interview alignment + per-question code sessions (RB38 handoff) - schema.
-- Idempotent: every change is guarded, so running this twice is a no-op.
--
-- Adds:
--   IRF.IsInterviewerAlignmentRequired             bit NULL   (the rollout switch; NULL = today's page on every channel)
--   IRF.AITrialEnabled                             bit NULL   (NULL = trial banner off: every existing row)
--   InterviewerProfile.TrialOnboardingAcknowledged bit NULL
--   InterviewerProfile.TrialOnboardingAcknowledgedOn datetime NULL
--   InterviewerProfile.TrialOnboardingPolicyVersion varchar(50) NULL
--   TechSkillComments.IsCodingQuestion             bit NULL
--   TechSkillComments.CodingTimeLimitInMinutes     int NULL
--   InterviewCodeSessions                          (new table)
--   InterviewJoinGateOverrides                     (new table)
-- SkillChannelSampleQuestions is NOT touched: it already has IsCodingQuestion / CodingTimeLimitInMinutes
-- for the AI interview, and that flow is unchanged.
--
-- Deployment order (columns must exist before the procedures that read them are created):
--   1. this script (or Tables/IRF.sql, Tables/InterviewerProfile.sql, Tables/TechSkillComments.sql,
--      Tables/InterviewCodeSessions.sql, Tables/InterviewJoinGateOverrides.sql)
--   2. Functions/fn_InterviewerCompletedInterviewsOnIrf.sql
--   3. StoredProcedures/usp_GetTrialJudgmentContext.sql      (now reads the shared function)
--   4. StoredProcedures/usp_FetchAudioComment.sql             (now also returns IsInterviewerAlignmentRequired)
--      StoredProcedures/usp_UpdateIRFDetails.sql             (now saves IsInterviewerAlignmentRequired from Edit IRF)
--      StoredProcedures/usp_GetInterviewAlignmentRequired.sql
--      StoredProcedures/usp_GetInterviewAlignmentContext.sql
--      StoredProcedures/usp_AcknowledgeTrialOnboarding.sql
--      StoredProcedures/usp_SaveJoinGateOverride.sql
--      StoredProcedures/usp_GetInterviewerCodeQuestions.sql
--      StoredProcedures/usp_CreateInterviewCodeSession.sql
--      StoredProcedures/usp_GetInterviewCodeSessions.sql
--      StoredProcedures/usp_GetInterviewCodeSessionByToken.sql
--      StoredProcedures/usp_MarkInterviewCodeSessionOpened.sql
--      StoredProcedures/usp_SaveInterviewCodeSessionRun.sql
--      StoredProcedures/usp_SubmitInterviewCodeSession.sql
--   5. StoredProcedures/usp_AddTechSkillComments.sql         (now keeps the coding flag across saves)
-- The web code degrades to today's page if any of these objects are missing, so it can ship first.
-- Confirm which branch deploys (dev2026 vs master) before running this against a shared database.

-- Part one --------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsInterviewerAlignmentRequired' AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF ADD IsInterviewerAlignmentRequired bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AITrialEnabled' AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF ADD AITrialEnabled bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingAcknowledged' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile ADD TrialOnboardingAcknowledged bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingAcknowledgedOn' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile ADD TrialOnboardingAcknowledgedOn datetime NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingPolicyVersion' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile ADD TrialOnboardingPolicyVersion varchar(50) NULL
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewJoinGateOverrides')
BEGIN
    CREATE TABLE [dbo].[InterviewJoinGateOverrides] (
        [Id]                      int IDENTITY(1,1) NOT NULL,
        [InterviewId]             int NOT NULL,
        [InterviewerEmail]        varchar(200) NOT NULL,
        [OutstandingRequirements] varchar(500) NULL,
        [CreatedOn]               datetime NOT NULL CONSTRAINT [DF_IJGO_CreatedOn] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_InterviewJoinGateOverrides] PRIMARY KEY CLUSTERED ([Id] ASC)
    )

    CREATE NONCLUSTERED INDEX [IX_IJGO_InterviewId] ON [dbo].[InterviewJoinGateOverrides]([InterviewId])
END
GO

-- Part two --------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsCodingQuestion' AND Object_ID = Object_ID(N'TechSkillComments'))
BEGIN
    ALTER TABLE TechSkillComments ADD IsCodingQuestion bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CodingTimeLimitInMinutes' AND Object_ID = Object_ID(N'TechSkillComments'))
BEGIN
    ALTER TABLE TechSkillComments ADD CodingTimeLimitInMinutes int NULL
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewCodeSessions')
BEGIN
    CREATE TABLE [dbo].[InterviewCodeSessions] (
        [Id]                       int IDENTITY(1,1) NOT NULL,
        [InterviewId]              int NOT NULL,
        [QuestionId]               int NOT NULL,
        [QuestionText]             nvarchar(max) NOT NULL,
        [Token]                    uniqueidentifier NOT NULL,
        [LanguageId]               varchar(40) NULL,
        [SubmittedSource]          nvarchar(max) NULL,
        [LastRunSource]            nvarchar(max) NULL,
        [LastRunOutput]            nvarchar(max) NULL,
        [LastRunStatus]            varchar(40) NULL,
        [RunCount]                 int NOT NULL CONSTRAINT [DF_ICS_RunCount] DEFAULT (0),
        [CodingTimeLimitInMinutes] int NULL,
        [CreatedBy]                varchar(200) NULL,
        [CreatedOn]                datetime NOT NULL CONSTRAINT [DF_ICS_CreatedOn] DEFAULT (GETUTCDATE()),
        [OpenedOn]                 datetime NULL,
        [SubmittedOn]              datetime NULL,
        CONSTRAINT [PK_InterviewCodeSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_ICS_Token] UNIQUE ([Token]),
        CONSTRAINT [UQ_ICS_InterviewQuestion] UNIQUE ([InterviewId], [QuestionId])
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_ICS_InterviewId' AND object_id = OBJECT_ID(N'dbo.InterviewCodeSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ICS_InterviewId] ON [dbo].[InterviewCodeSessions]([InterviewId])
END
GO
