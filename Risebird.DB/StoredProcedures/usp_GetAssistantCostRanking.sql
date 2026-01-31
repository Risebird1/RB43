-- =============================================
-- Get Assistant Cost Ranking
-- Returns assistants ranked by cost with detailed metrics
-- =============================================

CREATE OR ALTER PROCEDURE [dbo].[usp_GetAssistantCostRanking]
    @TopN INT = NULL,
    @MinCost DECIMAL(18,6) = NULL,
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- If date range specified, calculate from logs directly
    IF @StartDate IS NOT NULL OR @EndDate IS NOT NULL
    BEGIN
        SELECT TOP (ISNULL(@TopN, 2147483647))
            AssistantId,
            
            -- Most commonly used model
            (
                SELECT TOP 1 Model 
                FROM OpenAIApiLogs logs2 
                WHERE logs2.AssistantId = logs.AssistantId 
                    AND logs2.Model IS NOT NULL
                    AND (@StartDate IS NULL OR logs2.LogDate >= @StartDate)
                    AND (@EndDate IS NULL OR logs2.LogDate <= @EndDate)
                GROUP BY Model 
                ORDER BY COUNT(*) DESC
            ) as Model,
            
            -- Primary operation type
            (
                SELECT TOP 1 OperationType 
                FROM OpenAIApiLogs logs3 
                WHERE logs3.AssistantId = logs.AssistantId 
                    AND logs3.OperationType IS NOT NULL
                    AND (@StartDate IS NULL OR logs3.LogDate >= @StartDate)
                    AND (@EndDate IS NULL OR logs3.LogDate <= @EndDate)
                GROUP BY OperationType 
                ORDER BY COUNT(*) DESC
            ) as PrimaryOperationType,
            
            COUNT(*) as TotalCalls,
            SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessfulCalls,
            COUNT(DISTINCT RunId) as CompletedRuns,
            ISNULL(SUM(TotalTokens), 0) as TotalTokens,
            ISNULL(SUM(EstimatedCostUSD), 0) as TotalCostUSD,
            CASE 
                WHEN COUNT(DISTINCT RunId) > 0 
                THEN ISNULL(SUM(EstimatedCostUSD), 0) / COUNT(DISTINCT RunId)
                ELSE 0 
            END as AvgCostPerRun,
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
        -- Use summary table for better performance
        SELECT TOP (ISNULL(@TopN, 2147483647))
            AssistantId,
            AssistantName,
            Model,
            PrimaryOperationType,
            TotalCalls,
            SuccessfulCalls,
            FailedCalls,
            CompletedRuns,
            TotalPromptTokens,
            TotalCompletionTokens,
            TotalTokens,
            TotalCostUSD,
            AvgCostPerCall,
            AvgCostPerRun,
            FirstUsedDate,
            LastUsedDate,
            DaysActive,
            AvgResponseTimeMs,
            LastUpdated
        FROM OpenAIAssistantCostSummary
        WHERE (@MinCost IS NULL OR TotalCostUSD >= @MinCost)
        ORDER BY TotalCostUSD DESC;
    END
END
GO

PRINT 'Stored procedure usp_GetAssistantCostRanking created/updated successfully.';
