IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PlivoWhatsAppLogs')
BEGIN
    CREATE TABLE [dbo].[PlivoWhatsAppLogs]
    (
        [Id]                        INT            IDENTITY(1,1) NOT NULL,

        -- ── Candidate context ──────────────────────────────────────────────────────
        [RecipientName]             NVARCHAR(200)  NULL,
        [RecipientPhone]            NVARCHAR(50)   NOT NULL,
        [RecipientEmail]            NVARCHAR(200)  NULL,
        [IRFId]                     INT            NULL,
        [CandidateId]               INT            NULL,
        [JobTitle]                  NVARCHAR(200)  NULL,
        [ClientName]                NVARCHAR(200)  NULL,
        [MessagePurpose]            NVARCHAR(20)   NOT NULL
            CONSTRAINT [DF_PlivoWhatsAppLogs_MessagePurpose]   DEFAULT ('schedule'),  -- 'schedule' | 'reschedule'
        [OriginalInterviewDate]     NVARCHAR(100)  NULL,

        -- ── Plivo CX conversation ──────────────────────────────────────────────────
        [PlivoCXConversationId]     NVARCHAR(200)  NULL,
        [PlivoCXRequestId]          NVARCHAR(200)  NULL,
        [MessageStatus]             NVARCHAR(50)   NULL
            CONSTRAINT [DF_PlivoWhatsAppLogs_MessageStatus]    DEFAULT ('initiated'),
            -- initiated | triggered | trigger-failed | completed

        -- ── AI-extracted conversation output ───────────────────────────────────────
        [ConversationTranscript]    NVARCHAR(MAX)  NULL,
        [ConversationSummary]       NVARCHAR(MAX)  NULL,
        [AIReportedInterviewDate]   NVARCHAR(100)  NULL,
        [AIReportedInterviewTime]   NVARCHAR(50)   NULL,
        [AIReportedBooked]          BIT            NOT NULL
            CONSTRAINT [DF_PlivoWhatsAppLogs_AIReportedBooked] DEFAULT (0),

        -- ── Booking outcome ────────────────────────────────────────────────────────
        [InterviewBooked]           BIT            NOT NULL
            CONSTRAINT [DF_PlivoWhatsAppLogs_InterviewBooked]  DEFAULT (0),
        [BookingApiSuccess]         BIT            NOT NULL
            CONSTRAINT [DF_PlivoWhatsAppLogs_BookingApiSuccess] DEFAULT (0),
        [BookingApiResponse]        NVARCHAR(MAX)  NULL,
        [BookedInterviewDate]       NVARCHAR(100)  NULL,
        [BookedInterviewTime]       NVARCHAR(100)  NULL,
        [BookedInterviewDateTime]   DATETIME       NULL,

        -- ── Overall status and disposition ────────────────────────────────────────
        [Status]                    NVARCHAR(50)   NOT NULL
            CONSTRAINT [DF_PlivoWhatsAppLogs_Status]           DEFAULT ('initiated'),
            -- initiated | triggered | trigger-failed | completed

        [CandidateDisposition]      NVARCHAR(50)   NULL,
            -- interested | uninterested | cant_say

        [ConversationEndedAt]       DATETIME       NULL,

        -- ── Audit ─────────────────────────────────────────────────────────────────
        [TriggeredBy]               NVARCHAR(200)  NULL,
        [CreatedAt]                 DATETIME       NOT NULL
            CONSTRAINT [DF_PlivoWhatsAppLogs_CreatedAt]        DEFAULT (GETUTCDATE()),
        [UpdatedAt]                 DATETIME       NULL,

        CONSTRAINT [PK_PlivoWhatsAppLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO
