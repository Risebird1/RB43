

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'VapiWebhookDebugLogs')
BEGIN 
-- Table to debug and monitor all Vapi webhook requests
CREATE TABLE [dbo].[VapiWebhookDebugLogs]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestTimestamp] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [WebhookType] NVARCHAR(100) NULL,
    [CallId] NVARCHAR(100) NULL,
    [AssistantId] NVARCHAR(100) NULL,
    [ToolCallName] NVARCHAR(100) NULL,
    [RawPayload] NVARCHAR(MAX) NULL,
    [ProcessingStatus] NVARCHAR(50) NULL, -- success, error, skipped
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CallLogFound] BIT NULL,
    [CallLogId] INT NULL,
    [DataSaved] BIT NULL,
    
    INDEX [IX_VapiWebhookDebugLogs_CallId] NONCLUSTERED ([CallId]),
    INDEX [IX_VapiWebhookDebugLogs_RequestTimestamp] NONCLUSTERED ([RequestTimestamp] DESC),
    INDEX [IX_VapiWebhookDebugLogs_ToolCallName] NONCLUSTERED ([ToolCallName])
);

End
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Debug logs for all Vapi webhook requests to troubleshoot integration issues', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'VapiWebhookDebugLogs';
GO
