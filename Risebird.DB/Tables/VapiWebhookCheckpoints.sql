-- Table to store detailed webhook processing checkpoints for debugging
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'VapiWebhookCheckpoints')
BEGIN
    CREATE TABLE [dbo].[VapiWebhookCheckpoints]
    (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Timestamp] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [CheckpointName] NVARCHAR(50) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [CallId] NVARCHAR(100) NULL,
        [ToolCallName] NVARCHAR(100) NULL,
        [AdditionalData] NVARCHAR(MAX) NULL,
        
        INDEX [IX_VapiWebhookCheckpoints_Timestamp] NONCLUSTERED ([Timestamp] DESC),
        INDEX [IX_VapiWebhookCheckpoints_CallId] NONCLUSTERED ([CallId]),
        INDEX [IX_VapiWebhookCheckpoints_CheckpointName] NONCLUSTERED ([CheckpointName])
    );
END
GO

-- Add description
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores detailed checkpoint logs for webhook processing flow debugging', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'VapiWebhookCheckpoints';
GO
