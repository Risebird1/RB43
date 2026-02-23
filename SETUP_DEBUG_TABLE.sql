-- =============================================
-- Vapi Webhook Debug Table Setup
-- Run this script to enable webhook debugging
-- =============================================

-- Check if table already exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VapiWebhookDebugLogs')
BEGIN
    PRINT 'VapiWebhookDebugLogs table already exists. Skipping creation.'
END
ELSE
BEGIN
    PRINT 'Creating VapiWebhookDebugLogs table...'
    
    CREATE TABLE [dbo].[VapiWebhookDebugLogs]
    (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [RequestTimestamp] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [WebhookType] NVARCHAR(100) NULL,
        [CallId] NVARCHAR(100) NULL,
        [AssistantId] NVARCHAR(100) NULL,
        [ToolCallName] NVARCHAR(100) NULL,
        [RawPayload] NVARCHAR(MAX) NULL,
        [ProcessingStatus] NVARCHAR(50) NULL, -- success, error, skipped, received, processing
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [CallLogFound] BIT NULL,
        [CallLogId] INT NULL,
        [DataSaved] BIT NULL,
        
        INDEX [IX_VapiWebhookDebugLogs_CallId] NONCLUSTERED ([CallId]),
        INDEX [IX_VapiWebhookDebugLogs_RequestTimestamp] NONCLUSTERED ([RequestTimestamp] DESC),
        INDEX [IX_VapiWebhookDebugLogs_ToolCallName] NONCLUSTERED ([ToolCallName])
    );
    
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Debug logs for all Vapi webhook requests to troubleshoot integration issues', 
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'VapiWebhookDebugLogs';
    
    PRINT 'VapiWebhookDebugLogs table created successfully!'
END
GO

-- Verify table exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VapiWebhookDebugLogs')
BEGIN
    PRINT 'SUCCESS: VapiWebhookDebugLogs table is ready to use.'
    
    -- Show table structure
    SELECT 
        COLUMN_NAME, 
        DATA_TYPE, 
        CHARACTER_MAXIMUM_LENGTH,
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'VapiWebhookDebugLogs'
    ORDER BY ORDINAL_POSITION;
END
ELSE
BEGIN
    PRINT 'ERROR: Failed to create VapiWebhookDebugLogs table.'
END
GO
