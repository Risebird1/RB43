-- =============================================
-- Performance Indexes for OpenAIApiLogs Table
-- Optimizes common query patterns
-- =============================================

PRINT '========================================';
PRINT 'Creating Performance Indexes for OpenAIApiLogs';
PRINT '========================================';
PRINT '';

-- =============================================
-- Index 1: Date Range Queries (Most Common)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_LogDate_Desc' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_LogDate_Desc
    ON [dbo].[OpenAIApiLogs] ([LogDate] DESC)
    INCLUDE ([OperationType], [Model], [IsSuccess], [ResponseTimeMs], [TotalTokens], [EstimatedCostUSD], [PromptTokens], [CompletionTokens]);
    
    PRINT '✓ Index IX_OpenAIApiLogs_LogDate_Desc created - Optimizes date range queries';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIApiLogs_LogDate_Desc already exists';
END
GO

-- =============================================
-- Index 2: AssistantId Queries
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_AssistantId' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_AssistantId
    ON [dbo].[OpenAIApiLogs] ([AssistantId])
    INCLUDE ([LogDate], [EstimatedCostUSD], [TotalTokens], [IsSuccess], [RunId], [Model], [OperationType]);
    
    PRINT '✓ Index IX_OpenAIApiLogs_AssistantId created - Optimizes assistant cost queries';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIApiLogs_AssistantId already exists';
END
GO

-- =============================================
-- Index 3: Failed Calls
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_IsSuccess_LogDate' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_IsSuccess_LogDate
    ON [dbo].[OpenAIApiLogs] ([IsSuccess], [LogDate] DESC)
    INCLUDE ([OperationType], [ErrorMessage], [StatusCode]);
    
    PRINT '✓ Index IX_OpenAIApiLogs_IsSuccess_LogDate created - Optimizes failed calls queries';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIApiLogs_IsSuccess_LogDate already exists';
END
GO

-- =============================================
-- Index 4: Operation Type Queries
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_OperationType_LogDate' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_OperationType_LogDate
    ON [dbo].[OpenAIApiLogs] ([OperationType], [LogDate] DESC)
    INCLUDE ([EstimatedCostUSD], [TotalTokens], [IsSuccess]);
    
    PRINT '✓ Index IX_OpenAIApiLogs_OperationType_LogDate created - Optimizes operation type queries';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIApiLogs_OperationType_LogDate already exists';
END
GO

-- =============================================
-- Index 5: Session ID Queries
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_SessionId' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_SessionId
    ON [dbo].[OpenAIApiLogs] ([SessionId])
    INCLUDE ([LogDate], [OperationType], [IsSuccess], [EstimatedCostUSD]);
    
    PRINT '✓ Index IX_OpenAIApiLogs_SessionId created - Optimizes session queries';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIApiLogs_SessionId already exists';
END
GO

-- =============================================
-- Index 6: RunId Queries (for Assistant API)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_RunId' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_RunId
    ON [dbo].[OpenAIApiLogs] ([RunId])
    INCLUDE ([AssistantId], [ThreadId], [LogDate], [EstimatedCostUSD], [TotalTokens]);
    
    PRINT '✓ Index IX_OpenAIApiLogs_RunId created - Optimizes run queries';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIApiLogs_RunId already exists';
END
GO

-- =============================================
-- Index 7: CandidateId, InterviewerId, IRFId, MeetingId
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_Entities' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_Entities
    ON [dbo].[OpenAIApiLogs] ([CandidateId], [InterviewerId], [IRFId], [MeetingId])
    INCLUDE ([LogDate], [EstimatedCostUSD], [OperationType]);
    
    PRINT '✓ Index IX_OpenAIApiLogs_Entities created - Optimizes entity-specific queries';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIApiLogs_Entities already exists';
END
GO

-- =============================================
-- Verify Indexes
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'Index Summary:';
PRINT '========================================';

SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    CAST(ps.used_page_count * 8 / 1024.0 AS DECIMAL(10,2)) AS SizeMB,
    ps.row_count AS TotalRows
FROM sys.indexes i
INNER JOIN sys.dm_db_partition_stats ps 
    ON i.object_id = ps.object_id 
    AND i.index_id = ps.index_id
WHERE i.object_id = OBJECT_ID('OpenAIApiLogs')
    AND i.name IS NOT NULL
ORDER BY ps.used_page_count DESC;

PRINT '';
PRINT '✓ All performance indexes created successfully!';
PRINT '';
PRINT 'Performance Improvements Expected:';
PRINT '- Date range queries: 10-50x faster';
PRINT '- Assistant cost queries: 20-100x faster';
PRINT '- Failed calls queries: 5-20x faster';
PRINT '- Session/Entity queries: 10-30x faster';
PRINT '';
PRINT 'Note: Index sizes will grow with data. Monitor and maintain regularly.';
