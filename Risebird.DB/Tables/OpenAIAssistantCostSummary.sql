-- =============================================
-- OpenAI Assistant Cost Summary Table
-- Aggregates costs by AssistantId for quick reporting
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OpenAIAssistantCostSummary]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OpenAIAssistantCostSummary] (
        [AssistantId] NVARCHAR(100) NOT NULL PRIMARY KEY,
        [AssistantName] NVARCHAR(200) NULL,
        [Model] NVARCHAR(100) NULL,
        [PrimaryOperationType] NVARCHAR(100) NULL,
        
        -- Call statistics
        [TotalCalls] INT NOT NULL DEFAULT 0,
        [SuccessfulCalls] INT NOT NULL DEFAULT 0,
        [FailedCalls] INT NOT NULL DEFAULT 0,
        [CompletedRuns] INT NOT NULL DEFAULT 0,
        
        -- Token usage
        [TotalPromptTokens] BIGINT NOT NULL DEFAULT 0,
        [TotalCompletionTokens] BIGINT NOT NULL DEFAULT 0,
        [TotalTokens] BIGINT NOT NULL DEFAULT 0,
        
        -- Cost tracking
        [TotalCostUSD] DECIMAL(18, 6) NOT NULL DEFAULT 0,
        [AvgCostPerCall] DECIMAL(18, 6) NULL,
        [AvgCostPerRun] DECIMAL(18, 6) NULL,
        
        -- Usage tracking
        [FirstUsedDate] DATETIME2(7) NULL,
        [LastUsedDate] DATETIME2(7) NULL,
        [DaysActive] INT NULL,
        
        -- Performance metrics
        [AvgResponseTimeMs] INT NULL,
        [TotalResponseTimeMs] BIGINT NULL,
        
        -- Metadata
        [LastUpdated] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()
    );

    PRINT 'Table OpenAIAssistantCostSummary created successfully.';
END
ELSE
BEGIN
    PRINT 'Table OpenAIAssistantCostSummary already exists.';
END
GO

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIAssistantCostSummary_TotalCost' AND object_id = OBJECT_ID('OpenAIAssistantCostSummary'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIAssistantCostSummary_TotalCost
    ON [dbo].[OpenAIAssistantCostSummary] ([TotalCostUSD] DESC)
    INCLUDE ([AssistantName], [Model], [TotalCalls]);
    
    PRINT 'Index IX_OpenAIAssistantCostSummary_TotalCost created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIAssistantCostSummary_LastUsed' AND object_id = OBJECT_ID('OpenAIAssistantCostSummary'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIAssistantCostSummary_LastUsed
    ON [dbo].[OpenAIAssistantCostSummary] ([LastUsedDate] DESC);
    
    PRINT 'Index IX_OpenAIAssistantCostSummary_LastUsed created.';
END
GO

PRINT 'OpenAIAssistantCostSummary table setup complete.';
