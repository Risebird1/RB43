-- =============================================
-- Migration: Create PhoneScreenCalls table
-- Date: 2026-07-21
-- Description: Stores the outcome of every AI "Phone Screener" call placed to a
--              candidate in a skill channel (IRF). The schema is provider-agnostic
--              and mirrors the normalized call-result contract used by the UI
--              (provider, status, endedReason, recordingUrl, transcript, summary,
--              successEvaluation, structuredData, raw, etc.) so any voice platform
--              (Plivo today, VAPI/others tomorrow) can plug in via a backend adapter
--              without UI or schema changes.
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PhoneScreenCalls]') AND type = N'U')
BEGIN
    PRINT 'Creating PhoneScreenCalls table...';

    CREATE TABLE [dbo].[PhoneScreenCalls]
    (
        [Id]                INT IDENTITY(1,1) NOT NULL,

        -- Provider / routing
        [Provider]          VARCHAR(30)     NULL,          -- 'plivo', 'vapi', ...
        [IRFId]             INT             NOT NULL,       -- IRF.id (skill channel)
        [CandidateId]       INT             NULL,           -- Candidate.id
        [CandidateMasterId] INT             NULL,           -- CandidatesMaster.id

        -- Candidate snapshot (denormalized for display resilience)
        [CandidateName]     NVARCHAR(200)   NULL,
        [CandidateEmail]    NVARCHAR(200)   NULL,
        [Phone]             VARCHAR(30)      NULL,

        -- Provider call identifiers
        [ExternalCallId]    VARCHAR(200)     NULL,          -- Plivo CallUUID / VAPI call id
        [ConversationId]    VARCHAR(200)     NULL,          -- Plivo CX conversation id

        -- Lifecycle
        [Status]            VARCHAR(40)      NULL,          -- initiated|triggered|trigger-failed|queued|ringing|in-progress|ended|no_answer|busy_rejected|failed|completed
        [EndedReason]       NVARCHAR(300)    NULL,
        [StartedAt]         DATETIME         NULL,
        [EndedAt]           DATETIME         NULL,
        [DurationSeconds]   INT              NULL,
        [Cost]              DECIMAL(10,4)    NULL,

        -- Artifacts
        [RecordingUrl]      VARCHAR(1000)    NULL,
        [Transcript]        NVARCHAR(MAX)    NULL,
        [Summary]           NVARCHAR(MAX)    NULL,

        -- Evaluation / disposition (screener specific but stored generically)
        [SuccessEvaluation] NVARCHAR(100)    NULL,          -- PASS / FAIL / score / free text
        [Disposition]       NVARCHAR(100)    NULL,
        [Qualified]         VARCHAR(10)      NULL,          -- yes / no
        [FailedCriteria]    NVARCHAR(300)    NULL,
        [Availability]      NVARCHAR(500)    NULL,

        -- Arbitrary structured output + raw provider payload (both JSON strings)
        [StructuredData]    NVARCHAR(MAX)    NULL,
        [RawPayload]        NVARCHAR(MAX)    NULL,

        -- Audit
        [CreatedAt]         DATETIME         NOT NULL CONSTRAINT [DF_PhoneScreenCalls_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]         DATETIME         NULL,
        [CreatedBy]         VARCHAR(150)     NULL,

        CONSTRAINT [PK_PhoneScreenCalls] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_PhoneScreenCalls_IRFId]          ON [dbo].[PhoneScreenCalls] ([IRFId] ASC);
    CREATE NONCLUSTERED INDEX [IX_PhoneScreenCalls_CandidateId]    ON [dbo].[PhoneScreenCalls] ([CandidateId] ASC);
    CREATE NONCLUSTERED INDEX [IX_PhoneScreenCalls_ConversationId] ON [dbo].[PhoneScreenCalls] ([ConversationId] ASC);

    PRINT 'PhoneScreenCalls table created.';
END
ELSE
BEGIN
    PRINT 'PhoneScreenCalls table already exists. Skipping.';
END
GO
