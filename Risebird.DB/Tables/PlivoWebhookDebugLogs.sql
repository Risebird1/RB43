-- =============================================================================
-- PlivoWebhookDebugLogs
-- Stores raw payloads and processing status for every callback/event received
-- from Plivo (hangup, ring, stream-status) for debugging and auditing.
-- =============================================================================
CREATE TABLE [dbo].[PlivoWebhookDebugLogs] (
    [Id]                INT IDENTITY(1,1)   NOT NULL,
    [RequestTimestamp]  DATETIME            NOT NULL    DEFAULT GETUTCDATE(),
    [WebhookType]       NVARCHAR(50)        NULL,   -- hangup / ring / stream-status / answer
    [CallUUID]          NVARCHAR(100)       NULL,
    [RequestUUID]       NVARCHAR(100)       NULL,
    [RawPayload]        NVARCHAR(MAX)       NULL,
    [ProcessingStatus]  NVARCHAR(50)        NULL,   -- received / processing / success / error / skipped
    [ErrorMessage]      NVARCHAR(MAX)       NULL,
    [CallLogFound]      BIT                 NULL,
    [CallLogId]         INT                 NULL,
    [DataSaved]         BIT                 NULL,

    CONSTRAINT [PK_PlivoWebhookDebugLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_PlivoWebhookDebugLogs_CallUUID]
    ON [dbo].[PlivoWebhookDebugLogs] ([CallUUID])
    WHERE [CallUUID] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_PlivoWebhookDebugLogs_RequestTimestamp]
    ON [dbo].[PlivoWebhookDebugLogs] ([RequestTimestamp] DESC);
GO
