-- SupportTicketProcessingLog
-- Debug/checkpoint table for tracking resume ingestion and scheduling flow.
-- Helps diagnose failures and understand processing steps.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SupportTicketProcessingLog')
BEGIN
    CREATE TABLE [dbo].[SupportTicketProcessingLog] (
        [Id]                INT             IDENTITY(1,1) NOT NULL,
        [SupportTicketId]   INT             NULL,
        [PendingResumeId]   INT             NULL,
        [CandidateResumeId] INT             NULL,
        [Checkpoint]        VARCHAR(100)    NOT NULL,
        [Status]            VARCHAR(20)     NOT NULL,  -- 'Started', 'Success', 'Failed', 'Warning'
        [Message]           NVARCHAR(MAX)   NULL,
        [Details]           NVARCHAR(MAX)   NULL,      -- JSON with extra context
        [ProcessedBy]       VARCHAR(150)    NULL,
        [CreatedAt]         DATETIME        NOT NULL DEFAULT GETUTCDATE(),
        [DurationMs]        INT             NULL,      -- Time taken for this step
        CONSTRAINT [PK_SupportTicketProcessingLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE NONCLUSTERED INDEX [IX_STPL_TicketId] ON [dbo].[SupportTicketProcessingLog] ([SupportTicketId]);
    CREATE NONCLUSTERED INDEX [IX_STPL_PendingResumeId] ON [dbo].[SupportTicketProcessingLog] ([PendingResumeId]);
    CREATE NONCLUSTERED INDEX [IX_STPL_CreatedAt] ON [dbo].[SupportTicketProcessingLog] ([CreatedAt] DESC);
    CREATE NONCLUSTERED INDEX [IX_STPL_Checkpoint] ON [dbo].[SupportTicketProcessingLog] ([Checkpoint]);
END
GO
