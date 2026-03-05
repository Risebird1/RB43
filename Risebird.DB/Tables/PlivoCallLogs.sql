-- =============================================================================
-- PlivoCallLogs
-- Tracks every outbound AI interview call made via Plivo + OpenAI Realtime API.
-- Mirrors the structure of VapiCallLogs but uses Plivo-specific identifiers.
-- =============================================================================
CREATE TABLE [dbo].[PlivoCallLogs] (
    [Id]                        INT IDENTITY(1,1)   NOT NULL,

    -- Plivo call identifiers
    [PlivoCallUUID]             NVARCHAR(100)       NULL,   -- assigned by Plivo when call is answered
    [PlivoRequestUUID]          NVARCHAR(100)       NULL,   -- returned immediately when call is fired via REST API

    -- Relationships
    [InterviewId]               INT                 NULL,
    [IRFId]                     INT                 NULL,
    [RecipientId]               INT                 NULL,   -- Candidate.id

    -- Participant details
    [RecipientPhone]            NVARCHAR(20)        NOT NULL,   -- E.164 destination (candidate)
    [RecipientName]             NVARCHAR(255)       NULL,
    [RecipientEmail]            NVARCHAR(255)       NULL,
    [FromNumber]                NVARCHAR(20)        NULL,       -- Plivo caller ID (Indian number)

    -- Call lifecycle
    [CallStatus]                NVARCHAR(50)        NULL,   -- queued / ringing / in-progress / completed / no-answer / busy / failed
    [ScheduledDateTime]         DATETIME            NULL,
    [CallStartedAt]             DATETIME            NULL,
    [CallEndedAt]               DATETIME            NULL,
    [HangupCause]               NVARCHAR(100)       NULL,   -- Plivo hangup cause name
    [DurationSeconds]           INT                 NULL,

    -- AI conversation artefacts
    [Transcript]                NVARCHAR(MAX)       NULL,
    [RecordingUrl]              NVARCHAR(500)       NULL,
    [Summary]                   NVARCHAR(MAX)       NULL,
    [StructuredData]            NVARCHAR(MAX)       NULL,
    [AIModel]                   NVARCHAR(100)       NULL,   -- e.g. gpt-4o-realtime-preview

    -- Interview booking result
    [InterviewBooked]           BIT                 NULL,
    [BookedInterviewDateTime]   DATETIME            NULL,
    [BookingConfirmationDetails] NVARCHAR(MAX)      NULL,   -- JSON with full booking payload

    -- Billing
    [CallCost]                  DECIMAL(10, 4)      NULL,

    -- Audit
    [CreatedAt]                 DATETIME            NOT NULL    DEFAULT GETUTCDATE(),
    [UpdatedAt]                 DATETIME            NULL,
    [CreatedBy]                 NVARCHAR(255)       NULL,
    [UpdatedBy]                 NVARCHAR(255)       NULL,

    CONSTRAINT [PK_PlivoCallLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- Unique index on PlivoCallUUID (allows NULL — sparse index not supported in SQL Server so filter it)
CREATE UNIQUE NONCLUSTERED INDEX [IX_PlivoCallLogs_PlivoCallUUID]
    ON [dbo].[PlivoCallLogs] ([PlivoCallUUID])
    WHERE [PlivoCallUUID] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_PlivoCallLogs_PlivoRequestUUID]
    ON [dbo].[PlivoCallLogs] ([PlivoRequestUUID])
    WHERE [PlivoRequestUUID] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_PlivoCallLogs_InterviewId]
    ON [dbo].[PlivoCallLogs] ([InterviewId])
    WHERE [InterviewId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_PlivoCallLogs_IRFId]
    ON [dbo].[PlivoCallLogs] ([IRFId])
    WHERE [IRFId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_PlivoCallLogs_RecipientId]
    ON [dbo].[PlivoCallLogs] ([RecipientId])
    WHERE [RecipientId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_PlivoCallLogs_RecipientEmail_IRFId]
    ON [dbo].[PlivoCallLogs] ([RecipientEmail], [IRFId]);
GO

CREATE NONCLUSTERED INDEX [IX_PlivoCallLogs_CreatedAt]
    ON [dbo].[PlivoCallLogs] ([CreatedAt] DESC);
GO
