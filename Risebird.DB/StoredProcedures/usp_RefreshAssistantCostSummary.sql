-- =============================================
-- Refresh OpenAI Assistant Cost Summary
-- Aggregates all cost data by AssistantId
-- =============================================

CREATE OR ALTER PROCEDURE [dbo].[usp_RefreshAssistantCostSummary]
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Clear existing summary data
        TRUNCATE TABLE [dbo].[OpenAIAssistantCostSummary];
        
        -- Insert aggregated data
        INSERT INTO [dbo].[OpenAIAssistantCostSummary] (
            AssistantId,
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
            TotalResponseTimeMs,
            LastUpdated,
            CreatedAt
        )
        SELECT 
            AssistantId,
            
            -- Most commonly used model
            (
                SELECT TOP 1 Model 
                FROM OpenAIApiLogs logs2 
                WHERE logs2.AssistantId = logs.AssistantId 
                    AND logs2.Model IS NOT NULL
                GROUP BY Model 
                ORDER BY COUNT(*) DESC
            ) as Model,
            
            -- Most common operation type
            (
                SELECT TOP 1 OperationType 
                FROM OpenAIApiLogs logs3 
                WHERE logs3.AssistantId = logs.AssistantId 
                    AND logs3.OperationType IS NOT NULL
                GROUP BY OperationType 
                ORDER BY COUNT(*) DESC
            ) as PrimaryOperationType,
            
            -- Call statistics
            COUNT(*) as TotalCalls,
            SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessfulCalls,
            SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) as FailedCalls,
            COUNT(DISTINCT RunId) as CompletedRuns,
            
            -- Token usage
            ISNULL(SUM(PromptTokens), 0) as TotalPromptTokens,
            ISNULL(SUM(CompletionTokens), 0) as TotalCompletionTokens,
            ISNULL(SUM(TotalTokens), 0) as TotalTokens,
            
            -- Cost tracking
            ISNULL(SUM(EstimatedCostUSD), 0) as TotalCostUSD,
            ISNULL(AVG(EstimatedCostUSD), 0) as AvgCostPerCall,
            CASE 
                WHEN COUNT(DISTINCT RunId) > 0 
                THEN ISNULL(SUM(EstimatedCostUSD), 0) / COUNT(DISTINCT RunId)
                ELSE 0 
            END as AvgCostPerRun,
            
            -- Usage tracking
            MIN(LogDate) as FirstUsedDate,
            MAX(LogDate) as LastUsedDate,
            DATEDIFF(day, MIN(LogDate), MAX(LogDate)) + 1 as DaysActive,
            
            -- Performance metrics
            AVG(ResponseTimeMs) as AvgResponseTimeMs,
            SUM(CAST(ResponseTimeMs AS BIGINT)) as TotalResponseTimeMs,
            
            -- Metadata
            GETUTCDATE() as LastUpdated,
            GETUTCDATE() as CreatedAt
            
        FROM OpenAIApiLogs logs
        WHERE AssistantId IS NOT NULL
        GROUP BY AssistantId;
        
        COMMIT TRANSACTION;
        
        -- Return summary statistics
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
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

PRINT 'Stored procedure usp_RefreshAssistantCostSummary created/updated successfully.';
