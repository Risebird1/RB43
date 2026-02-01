-- =============================================
-- Get Assistant Cost Details
-- Returns detailed cost breakdown for a specific assistant
-- =============================================

CREATE OR ALTER PROCEDURE [dbo].[usp_GetAssistantCostDetails]
    @AssistantId NVARCHAR(100),
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Summary metrics
    SELECT 
        AssistantId,
        COUNT(*) as TotalCalls,
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
    
    -- Cost by operation type
    SELECT 
        OperationType,
        COUNT(*) as TotalCalls,
        COUNT(DISTINCT RunId) as CompletedRuns,
        ISNULL(SUM(TotalTokens), 0) as TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) as TotalCost,
        ISNULL(AVG(EstimatedCostUSD), 0) as AvgCost
    FROM OpenAIApiLogs
    WHERE AssistantId = @AssistantId
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY OperationType
    ORDER BY TotalCost DESC;
    
    -- Cost by model
    SELECT 
        Model,
        COUNT(*) as TotalCalls,
        ISNULL(SUM(TotalTokens), 0) as TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) as TotalCost
    FROM OpenAIApiLogs
    WHERE AssistantId = @AssistantId
        AND Model IS NOT NULL
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY Model
    ORDER BY TotalCost DESC;
    
    -- Daily cost trend
    SELECT 
        CAST(LogDate AS DATE) as Date,
        COUNT(*) as Calls,
        COUNT(DISTINCT RunId) as Runs,
        ISNULL(SUM(TotalTokens), 0) as Tokens,
        ISNULL(SUM(EstimatedCostUSD), 0) as Cost
    FROM OpenAIApiLogs
    WHERE AssistantId = @AssistantId
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY CAST(LogDate AS DATE)
    ORDER BY Date DESC;
END
GO

PRINT 'Stored procedure usp_GetAssistantCostDetails created/updated successfully.';
