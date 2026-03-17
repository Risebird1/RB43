-- Granular step-by-step checkpoint log for Plivo CX WhatsApp processing.
-- Each row captures one named step inside the ConversationEndCallback pipeline.
-- Query alongside PlivoWhatsAppDebugLogs to reconstruct exactly what happened
-- for any WhatsApp conversation.

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PlivoWhatsAppCheckpoints')
BEGIN
    CREATE TABLE [dbo].[PlivoWhatsAppCheckpoints]
    (
        [Id]               INT            IDENTITY(1,1)  NOT NULL
            CONSTRAINT [PK_PlivoWhatsAppCheckpoints] PRIMARY KEY CLUSTERED,

        [Timestamp]        DATETIME       NOT NULL DEFAULT GETUTCDATE(),

        -- Named step — e.g. "wa_callback_received", "wa_log_found", "wa_booking_done"
        [CheckpointName]   NVARCHAR(100)  NOT NULL,

        [Message]          NVARCHAR(MAX)  NOT NULL,

        -- Plivo CX conversation_id (if available)
        [ConversationId]   NVARCHAR(200)  NULL,

        -- FK to PlivoWhatsAppLogs.Id
        [LogId]            INT            NULL,

        -- Any extra JSON context (parsed field values, error detail, etc.)
        [AdditionalData]   NVARCHAR(MAX)  NULL,

        INDEX [IX_PlivoWhatsAppCheckpoints_Timestamp]      NONCLUSTERED ([Timestamp] DESC),
        INDEX [IX_PlivoWhatsAppCheckpoints_ConversationId] NONCLUSTERED ([ConversationId]),
        INDEX [IX_PlivoWhatsAppCheckpoints_CheckpointName] NONCLUSTERED ([CheckpointName]),
        INDEX [IX_PlivoWhatsAppCheckpoints_LogId]          NONCLUSTERED ([LogId])
    );
END
GO
