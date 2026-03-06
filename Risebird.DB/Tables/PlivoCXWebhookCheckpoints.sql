-- Granular step-by-step checkpoint log for Plivo CX webhook processing.
-- Each row captures one named step inside the HangupCallback / RecordingCallback pipeline.
-- Query this alongside PlivoCXWebhookDebugLogs to reconstruct exactly what happened for any call.

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PlivoCXWebhookCheckpoints')
BEGIN
    CREATE TABLE [dbo].[PlivoCXWebhookCheckpoints]
    (
        [Id]               INT IDENTITY(1,1)  PRIMARY KEY,
        [Timestamp]        DATETIME           NOT NULL DEFAULT GETUTCDATE(),
        [CheckpointName]   NVARCHAR(100)      NOT NULL,   -- e.g. "hangup_received", "call_log_found"
        [Message]          NVARCHAR(MAX)      NOT NULL,
        [ConversationId]   NVARCHAR(100)      NULL,       -- Plivo CX conversation_id (if available)
        [CallLogId]        INT                NULL,       -- FK to PlivoCXCallLogs.Id
        [AdditionalData]   NVARCHAR(MAX)      NULL,       -- any extra JSON context

        INDEX [IX_PlivoCXWebhookCheckpoints_Timestamp]      NONCLUSTERED ([Timestamp] DESC),
        INDEX [IX_PlivoCXWebhookCheckpoints_ConversationId] NONCLUSTERED ([ConversationId]),
        INDEX [IX_PlivoCXWebhookCheckpoints_CheckpointName] NONCLUSTERED ([CheckpointName]),
        INDEX [IX_PlivoCXWebhookCheckpoints_CallLogId]      NONCLUSTERED ([CallLogId])
    );
END
GO
