-- =============================================================================
-- PlivoCXWebhookDebugLogs
-- One row per:
--   • Trigger request  (LogType = 'trigger-request')
--   • Trigger response (LogType = 'trigger-response')
--   • Hangup callback  (LogType = 'hangup-callback')
--   • Recording cb     (LogType = 'recording-callback')
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = N'PlivoCXWebhookDebugLogs'
)
BEGIN
    CREATE TABLE [dbo].[PlivoCXWebhookDebugLogs] (
        [Id]               INT IDENTITY(1,1)  NOT NULL,
        [CreatedAt]        DATETIME           NOT NULL DEFAULT GETUTCDATE(),

        -- What kind of entry this is
        [LogType]          NVARCHAR(50)       NULL,   -- trigger-request | trigger-response | hangup-callback | recording-callback

        -- HTTP info
        [HttpStatusCode]   INT                NULL,   -- e.g. 200, 422

        -- Plivo CX identifiers (whatever is available at the time)
        [PlivoCXId]        NVARCHAR(255)      NULL,   -- api_id or conversation_id

        -- Our internal record
        [CallLogId]        INT                NULL,   -- FK to PlivoCXCallLogs.Id

        -- Full raw JSON payload
        [RawPayload]       NVARCHAR(MAX)      NULL,

        -- Processing outcome
        [ProcessingStatus] NVARCHAR(50)       NULL,   -- success | failed | error
        [ErrorMessage]     NVARCHAR(MAX)      NULL,

        CONSTRAINT [PK_PlivoCXWebhookDebugLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE INDEX [IX_PlivoCXWebhookDebugLogs_CallLogId]
        ON [dbo].[PlivoCXWebhookDebugLogs] ([CallLogId]);

    CREATE INDEX [IX_PlivoCXWebhookDebugLogs_CreatedAt]
        ON [dbo].[PlivoCXWebhookDebugLogs] ([CreatedAt] DESC);

    CREATE INDEX [IX_PlivoCXWebhookDebugLogs_PlivoCXId]
        ON [dbo].[PlivoCXWebhookDebugLogs] ([PlivoCXId]);
END
GO
