-- =============================================
-- Deploy Assistant Cost Tracking Feature
-- Run this script to add assistant-level cost tracking
-- =============================================

PRINT '========================================';
PRINT 'OpenAI Assistant Cost Tracking Deployment';
PRINT '========================================';
PRINT '';

-- =============================================
-- STEP 1: Create Summary Table
-- =============================================
PRINT 'Step 1: Creating OpenAIAssistantCostSummary table...';

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

    PRINT '✓ Table OpenAIAssistantCostSummary created successfully.';
END
ELSE
BEGIN
    PRINT '✓ Table OpenAIAssistantCostSummary already exists.';
END
GO

-- =============================================
-- STEP 2: Create Indexes
-- =============================================
PRINT '';
PRINT 'Step 2: Creating indexes...';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIAssistantCostSummary_TotalCost' AND object_id = OBJECT_ID('OpenAIAssistantCostSummary'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIAssistantCostSummary_TotalCost
    ON [dbo].[OpenAIAssistantCostSummary] ([TotalCostUSD] DESC)
    INCLUDE ([AssistantName], [Model], [TotalCalls]);
    
    PRINT '✓ Index IX_OpenAIAssistantCostSummary_TotalCost created.';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIAssistantCostSummary_TotalCost already exists.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIAssistantCostSummary_LastUsed' AND object_id = OBJECT_ID('OpenAIAssistantCostSummary'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIAssistantCostSummary_LastUsed
    ON [dbo].[OpenAIAssistantCostSummary] ([LastUsedDate] DESC);
    
    PRINT '✓ Index IX_OpenAIAssistantCostSummary_LastUsed created.';
END
ELSE
BEGIN
    PRINT '✓ Index IX_OpenAIAssistantCostSummary_LastUsed already exists.';
END
GO

-- =============================================
-- STEP 3: Create Stored Procedures
-- =============================================
PRINT '';
PRINT 'Step 3: Creating stored procedures...';
GO

-- Refresh Summary
CREATE OR ALTER PROCEDURE [dbo].[usp_RefreshAssistantCostSummary]
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        TRUNCATE TABLE [dbo].[OpenAIAssistantCostSummary];
        
        INSERT INTO [dbo].[OpenAIAssistantCostSummary] (
            AssistantId, Model, PrimaryOperationType, TotalCalls, SuccessfulCalls, FailedCalls, CompletedRuns,
            TotalPromptTokens, TotalCompletionTokens, TotalTokens, TotalCostUSD, AvgCostPerCall, AvgCostPerRun,
            FirstUsedDate, LastUsedDate, DaysActive, AvgResponseTimeMs, TotalResponseTimeMs, LastUpdated, CreatedAt
        )
        SELECT 
            AssistantId,
            (SELECT TOP 1 Model FROM OpenAIApiLogs logs2 WHERE logs2.AssistantId = logs.AssistantId AND logs2.Model IS NOT NULL GROUP BY Model ORDER BY COUNT(*) DESC) as Model,
            (SELECT TOP 1 OperationType FROM OpenAIApiLogs logs3 WHERE logs3.AssistantId = logs.AssistantId AND logs3.OperationType IS NOT NULL GROUP BY OperationType ORDER BY COUNT(*) DESC) as PrimaryOperationType,
            COUNT(*) as TotalCalls,
            SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessfulCalls,
            SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) as FailedCalls,
            COUNT(DISTINCT RunId) as CompletedRuns,
            ISNULL(SUM(PromptTokens), 0) as TotalPromptTokens,
            ISNULL(SUM(CompletionTokens), 0) as TotalCompletionTokens,
            ISNULL(SUM(TotalTokens), 0) as TotalTokens,
            ISNULL(SUM(EstimatedCostUSD), 0) as TotalCostUSD,
            ISNULL(AVG(EstimatedCostUSD), 0) as AvgCostPerCall,
            CASE WHEN COUNT(DISTINCT RunId) > 0 THEN ISNULL(SUM(EstimatedCostUSD), 0) / COUNT(DISTINCT RunId) ELSE 0 END as AvgCostPerRun,
            MIN(LogDate) as FirstUsedDate,
            MAX(LogDate) as LastUsedDate,
            DATEDIFF(day, MIN(LogDate), MAX(LogDate)) + 1 as DaysActive,
            AVG(ResponseTimeMs) as AvgResponseTimeMs,
            SUM(CAST(ResponseTimeMs AS BIGINT)) as TotalResponseTimeMs,
            GETUTCDATE() as LastUpdated,
            GETUTCDATE() as CreatedAt
        FROM OpenAIApiLogs logs
        WHERE AssistantId IS NOT NULL
        GROUP BY AssistantId;
        
        COMMIT TRANSACTION;
        
        SELECT 
            COUNT(*) as TotalAssistants,
            SUM(TotalCalls) as TotalCalls,
            SUM(CompletedRuns) as TotalRuns,
            SUM(TotalCostUSD) as TotalCost,
            MAX(LastUpdated) as LastUpdated
        FROM OpenAIAssistantCostSummary;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT '✓ Stored procedure usp_RefreshAssistantCostSummary created.';
GO

-- Get Ranking
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAssistantCostRanking]
    @TopN INT = NULL,
    @MinCost DECIMAL(18,6) = NULL,
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @StartDate IS NOT NULL OR @EndDate IS NOT NULL
    BEGIN
        SELECT TOP (ISNULL(@TopN, 2147483647))
            AssistantId,
            (SELECT TOP 1 Model FROM OpenAIApiLogs logs2 WHERE logs2.AssistantId = logs.AssistantId AND logs2.Model IS NOT NULL AND (@StartDate IS NULL OR logs2.LogDate >= @StartDate) AND (@EndDate IS NULL OR logs2.LogDate <= @EndDate) GROUP BY Model ORDER BY COUNT(*) DESC) as Model,
            (SELECT TOP 1 OperationType FROM OpenAIApiLogs logs3 WHERE logs3.AssistantId = logs.AssistantId AND logs3.OperationType IS NOT NULL AND (@StartDate IS NULL OR logs3.LogDate >= @StartDate) AND (@EndDate IS NULL OR logs3.LogDate <= @EndDate) GROUP BY OperationType ORDER BY COUNT(*) DESC) as PrimaryOperationType,
            COUNT(*) as TotalCalls,
            SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessfulCalls,
            COUNT(DISTINCT RunId) as CompletedRuns,
            ISNULL(SUM(TotalTokens), 0) as TotalTokens,
            ISNULL(SUM(EstimatedCostUSD), 0) as TotalCostUSD,
            CASE WHEN COUNT(DISTINCT RunId) > 0 THEN ISNULL(SUM(EstimatedCostUSD), 0) / COUNT(DISTINCT RunId) ELSE 0 END as AvgCostPerRun,
            MIN(LogDate) as FirstUsedDate,
            MAX(LogDate) as LastUsedDate,
            DATEDIFF(day, MIN(LogDate), MAX(LogDate)) + 1 as DaysActive,
            AVG(ResponseTimeMs) as AvgResponseTimeMs
        FROM OpenAIApiLogs logs
        WHERE AssistantId IS NOT NULL
            AND (@StartDate IS NULL OR LogDate >= @StartDate)
            AND (@EndDate IS NULL OR LogDate <= @EndDate)
        GROUP BY AssistantId
        HAVING (@MinCost IS NULL OR ISNULL(SUM(EstimatedCostUSD), 0) >= @MinCost)
        ORDER BY TotalCostUSD DESC;
    END
    ELSE
    BEGIN
        SELECT TOP (ISNULL(@TopN, 2147483647)) *
        FROM OpenAIAssistantCostSummary
        WHERE (@MinCost IS NULL OR TotalCostUSD >= @MinCost)
        ORDER BY TotalCostUSD DESC;
    END
END
GO

PRINT '✓ Stored procedure usp_GetAssistantCostRanking created.';
GO

-- Get Details
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAssistantCostDetails]
    @AssistantId NVARCHAR(100),
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Summary
    SELECT 
        AssistantId, COUNT(*) as TotalCalls,
        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessfulCalls,
        SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) as FailedCalls,
        COUNT(DISTINCT RunId) as CompletedRuns,
        ISNULL(SUM(PromptTokens), 0) as TotalPromptTokens,
        ISNULL(SUM(CompletionTokens), 0) as TotalCompletionTokens,
        ISNULL(SUM(TotalTokens), 0) as TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) as TotalCostUSD,
        ISNULL(AVG(EstimatedCostUSD), 0) as AvgCostPerCall,
        MIN(LogDate) as FirstUsedDate,
        MAX(LogDate) as LastUsedDate,
        AVG(ResponseTimeMs) as AvgResponseTimeMs
    FROM OpenAIApiLogs
    WHERE AssistantId = @AssistantId
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY AssistantId;
    
    -- By operation
    SELECT OperationType, COUNT(*) as TotalCalls, COUNT(DISTINCT RunId) as CompletedRuns,
        ISNULL(SUM(TotalTokens), 0) as TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) as TotalCost,
        ISNULL(AVG(EstimatedCostUSD), 0) as AvgCost
    FROM OpenAIApiLogs
    WHERE AssistantId = @AssistantId AND (@StartDate IS NULL OR LogDate >= @StartDate) AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY OperationType ORDER BY TotalCost DESC;
    
    -- By model
    SELECT Model, COUNT(*) as TotalCalls, ISNULL(SUM(TotalTokens), 0) as TotalTokens, ISNULL(SUM(EstimatedCostUSD), 0) as TotalCost
    FROM OpenAIApiLogs
    WHERE AssistantId = @AssistantId AND Model IS NOT NULL AND (@StartDate IS NULL OR LogDate >= @StartDate) AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY Model ORDER BY TotalCost DESC;
    
    -- Daily
    SELECT CAST(LogDate AS DATE) as Date, COUNT(*) as Calls, COUNT(DISTINCT RunId) as Runs,
        ISNULL(SUM(TotalTokens), 0) as Tokens, ISNULL(SUM(EstimatedCostUSD), 0) as Cost
    FROM OpenAIApiLogs
    WHERE AssistantId = @AssistantId AND (@StartDate IS NULL OR LogDate >= @StartDate) AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY CAST(LogDate AS DATE) ORDER BY Date DESC;
END
GO

PRINT '✓ Stored procedure usp_GetAssistantCostDetails created.';
GO

-- =============================================
-- STEP 4: Initial Data Load
-- =============================================
PRINT '';
PRINT 'Step 4: Loading initial data...';
GO

EXEC usp_RefreshAssistantCostSummary;
GO

-- =============================================
-- STEP 5: Verification
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'DEPLOYMENT COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'Verification:';

SELECT 
    'Total Assistants Tracked' as Metric,
    CAST(COUNT(*) AS VARCHAR) as Value
FROM OpenAIAssistantCostSummary

UNION ALL

SELECT 
    'Total Cost Tracked' as Metric,
    '$' + CAST(CAST(SUM(TotalCostUSD) AS DECIMAL(10,2)) AS VARCHAR) as Value
FROM OpenAIAssistantCostSummary

UNION ALL

SELECT 
    'Total API Calls' as Metric,
    CAST(SUM(TotalCalls) AS VARCHAR) as Value
FROM OpenAIAssistantCostSummary;

PRINT '';
PRINT 'Top 5 Most Expensive Assistants:';

SELECT TOP 5
    AssistantId,
    Model,
    PrimaryOperationType,
    TotalCalls,
    TotalCostUSD,
    CompletedRuns
FROM OpenAIAssistantCostSummary
ORDER BY TotalCostUSD DESC;

PRINT '';
PRINT '✓ Assistant Cost Tracking feature deployed successfully!';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Build and deploy updated code (Risebird.Entities, Repository, Web)';
PRINT '2. Access the new pages:';
PRINT '   - /OpenAILogs/AssistantRanking';
PRINT '   - /OpenAILogs/AssistantDetails/{assistantId}';
PRINT '3. Refresh summary data periodically using RefreshAssistantSummary button';
