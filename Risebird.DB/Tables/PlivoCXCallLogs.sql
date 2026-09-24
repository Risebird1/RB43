-- =============================================================================
-- PlivoCXCallLogs
-- Tracks every outbound interview call made via Plivo CX AI Agent platform.
--
-- Unlike the Programmable Voice approach, Plivo CX handles the entire AI
-- conversation (STT + LLM + TTS) internally.  Our server only:
--   1. Triggers the call via the CX API trigger endpoint
--   2. Receives the Hangup URL callback with extracted variables + transcript
-- =============================================================================
CREATE TABLE [dbo].[PlivoCXCallLogs] (
    [Id]                     INT IDENTITY(1,1)   NOT NULL,

    -- Plivo CX identifiers
    [PlivoCXConversationId]  NVARCHAR(255)       NULL,
    [PlivoCXRequestId]       NVARCHAR(255)       NULL,
    [PlivoCallUuid]          NVARCHAR(100)       NULL,   -- Plivo voice call UUID (from the hangup event)
    [RecordingUrl]           NVARCHAR(1000)      NULL,   -- Plivo recording URL (Recording API, by call UUID)

    -- Candidate context
    [RecipientName]          NVARCHAR(255)       NULL,
    [RecipientPhone]         NVARCHAR(50)        NULL,
    [RecipientEmail]         NVARCHAR(255)       NULL,
    [IRFId]                  INT                 NULL,
    [CandidateId]            INT                 NULL,
    [JobTitle]               NVARCHAR(255)       NULL,
    [ClientName]             NVARCHAR(255)       NULL,

    -- Call outcome
    [CallStatus]             NVARCHAR(50)        NULL,   -- initiated / triggered / answered / no-answer / failed
    [DurationSeconds]        INT                 NULL,
    [HangupCause]            NVARCHAR(255)       NULL,
    [CallTranscript]         NVARCHAR(MAX)       NULL,
    [ConversationSummary]    NVARCHAR(MAX)       NULL,

    -- Interview booking (extracted by AI Conversation node)
    [InterviewBooked]        BIT                 NOT NULL DEFAULT 0,
    [BookedInterviewDate]    NVARCHAR(100)       NULL,   -- raw AI-extracted value
    [BookedInterviewTime]    NVARCHAR(100)       NULL,   -- raw AI-extracted value
    [BookedInterviewDateTime] DATETIME           NULL,   -- parsed & normalised
    [BookingApiResponse]     NVARCHAR(MAX)       NULL,
    [BookingApiSuccess]      BIT                 NOT NULL DEFAULT 0,

    -- Candidate disposition (extracted by AI Conversation node)
    -- Values: 'interested' | 'uninterested' | 'cant_say'
    [CandidateDisposition]   NVARCHAR(20)        NULL,

    -- Purpose of the call
    -- Values: 'schedule' (first-time scheduling) | 'reschedule' (booking replacement for a missed/cancelled interview)
    [CallPurpose]            NVARCHAR(20)        NOT NULL DEFAULT 'schedule',

    -- Timestamps
    [CreatedAt]              DATETIME            NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]              DATETIME            NULL,
    [CallStartedAt]          DATETIME            NULL,
    [CallEndedAt]            DATETIME            NULL,
    [TriggeredBy]            NVARCHAR(255)       NULL,   -- "timer" or user email

    CONSTRAINT [PK_PlivoCXCallLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE INDEX [IX_PlivoCXCallLogs_ConversationId]
    ON [dbo].[PlivoCXCallLogs] ([PlivoCXConversationId]);
GO

CREATE INDEX [IX_PlivoCXCallLogs_IRFId_Email]
    ON [dbo].[PlivoCXCallLogs] ([IRFId], [RecipientEmail]);
GO
