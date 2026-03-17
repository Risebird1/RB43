-- Raw payload log for every Plivo CX WhatsApp trigger request/response and
-- ConversationEndCallback. One row per interaction — use alongside
-- PlivoWhatsAppCheckpoints to diagnose delivery failures and webhook issues.

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PlivoWhatsAppDebugLogs')
BEGIN
    CREATE TABLE [dbo].[PlivoWhatsAppDebugLogs]
    (
        [Id]               INT            IDENTITY(1,1)  NOT NULL
            CONSTRAINT [PK_PlivoWhatsAppDebugLogs] PRIMARY KEY CLUSTERED,

        [CreatedAt]        DATETIME       NOT NULL DEFAULT GETUTCDATE(),

        -- trigger-request | trigger-response | wa-callback
        [LogType]          NVARCHAR(100)  NOT NULL,

        -- HTTP status returned by the Plivo CX trigger API (NULL for inbound callbacks)
        [HttpStatusCode]   INT            NULL,

        -- Plivo CX api_id / conversation_id if available at log time
        [PlivoCXId]        NVARCHAR(200)  NULL,

        -- FK to PlivoWhatsAppLogs.Id (set when known)
        [LogId]            INT            NULL,

        -- Full JSON body sent or received
        [RawPayload]       NVARCHAR(MAX)  NULL,

        -- success | failed | received | error
        [ProcessingStatus] NVARCHAR(50)   NULL,

        [ErrorMessage]     NVARCHAR(MAX)  NULL,

        INDEX [IX_PlivoWhatsAppDebugLogs_CreatedAt] NONCLUSTERED ([CreatedAt] DESC),
        INDEX [IX_PlivoWhatsAppDebugLogs_LogId]     NONCLUSTERED ([LogId]),
        INDEX [IX_PlivoWhatsAppDebugLogs_PlivoCXId] NONCLUSTERED ([PlivoCXId])
    );
END
GO
