-- =============================================
-- Fix OpenAI API Cost Calculations
-- This script recalculates costs with correct gpt-4o pricing
-- Run this to fix historical data that was overestimated
-- =============================================

-- Backup the current costs before updating
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OpenAIApiLogs_CostBackup]') AND type in (N'U'))
BEGIN
    SELECT Id, EstimatedCostUSD, Model, PromptTokens, CompletionTokens
    INTO OpenAIApiLogs_CostBackup
    FROM OpenAIApiLogs;
    
    PRINT 'Backup created: OpenAIApiLogs_CostBackup';
END
GO

-- Function to calculate correct cost based on model and tokens
-- Pricing as of January 2026
CREATE OR ALTER FUNCTION dbo.fn_CalculateOpenAICost
(
    @Model NVARCHAR(100),
    @PromptTokens INT,
    @CompletionTokens INT
)
RETURNS DECIMAL(18, 6)
AS
BEGIN
    DECLARE @InputCostPer1K DECIMAL(18, 6) = 0.0025;  -- Default to gpt-4o
    DECLARE @OutputCostPer1K DECIMAL(18, 6) = 0.0100; -- Default to gpt-4o
    DECLARE @TotalCost DECIMAL(18, 6);
    
    SET @Model = LOWER(ISNULL(@Model, ''));
    
    -- GPT-4o-mini
    IF @Model LIKE '%gpt-4o-mini%'
    BEGIN
        SET @InputCostPer1K = 0.00015;   -- $0.15 per 1M
        SET @OutputCostPer1K = 0.00060;  -- $0.60 per 1M
    END
    -- GPT-4o (standard)
    ELSE IF @Model LIKE '%gpt-4o%'
    BEGIN
        SET @InputCostPer1K = 0.0025;    -- $2.50 per 1M
        SET @OutputCostPer1K = 0.0100;   -- $10.00 per 1M
    END
    -- GPT-4 Turbo
    ELSE IF @Model LIKE '%gpt-4-turbo%' OR @Model LIKE '%gpt-4-1106%' OR @Model LIKE '%gpt-4-0125%'
    BEGIN
        SET @InputCostPer1K = 0.01;      -- $10 per 1M
        SET @OutputCostPer1K = 0.03;     -- $30 per 1M
    END
    -- Legacy GPT-4
    ELSE IF @Model LIKE '%gpt-4%'
    BEGIN
        SET @InputCostPer1K = 0.03;      -- $30 per 1M
        SET @OutputCostPer1K = 0.06;     -- $60 per 1M
    END
    -- GPT-3.5 Turbo
    ELSE IF @Model LIKE '%gpt-3.5-turbo%'
    BEGIN
        SET @InputCostPer1K = 0.0015;    -- $1.50 per 1M
        SET @OutputCostPer1K = 0.002;    -- $2.00 per 1M
    END
    
    -- Calculate total cost
    SET @TotalCost = 
        (CAST(@PromptTokens AS DECIMAL(18, 6)) / 1000.0 * @InputCostPer1K) + 
        (CAST(@CompletionTokens AS DECIMAL(18, 6)) / 1000.0 * @OutputCostPer1K);
    
    RETURN ROUND(@TotalCost, 6);
END
GO

-- Show current vs corrected costs for gpt-4o models
PRINT '=== COST COMPARISON FOR GPT-4O MODELS ===';
SELECT TOP 10
    Id,
    Model,
    PromptTokens,
    CompletionTokens,
    EstimatedCostUSD AS OldCost_WRONG,
    dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens) AS NewCost_CORRECT,
    EstimatedCostUSD - dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens) AS Overestimation,
    CAST((EstimatedCostUSD / NULLIF(dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens), 0)) AS DECIMAL(5,1)) AS OverestimationMultiplier
FROM OpenAIApiLogs
WHERE Model LIKE '%gpt-4o%' 
    AND PromptTokens > 0
ORDER BY Id DESC;

-- Show total overestimation by model
PRINT '';
PRINT '=== TOTAL OVERESTIMATION BY MODEL ===';
SELECT 
    Model,
    COUNT(*) AS CallCount,
    SUM(EstimatedCostUSD) AS OldTotalCost_WRONG,
    SUM(dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens)) AS NewTotalCost_CORRECT,
    SUM(EstimatedCostUSD) - SUM(dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens)) AS TotalOverestimation,
    CAST(AVG(EstimatedCostUSD / NULLIF(dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens), 0)) AS DECIMAL(5,1)) AS AvgOverestimationMultiplier
FROM OpenAIApiLogs
WHERE Model IS NOT NULL 
    AND PromptTokens > 0
GROUP BY Model
ORDER BY TotalOverestimation DESC;

-- Update all records with correct costs
PRINT '';
PRINT '=== UPDATING ALL COSTS ===';

UPDATE OpenAIApiLogs
SET EstimatedCostUSD = dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens),
    ModifiedAt = GETUTCDATE()
WHERE Model IS NOT NULL 
    AND (PromptTokens > 0 OR CompletionTokens > 0)
    AND EstimatedCostUSD <> dbo.fn_CalculateOpenAICost(Model, PromptTokens, CompletionTokens);

PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records updated with correct costs.';
PRINT '';
PRINT 'Cost recalculation complete!';
PRINT 'Backup table: OpenAIApiLogs_CostBackup';
PRINT '';
PRINT '=== NEW TOTALS BY MODEL ===';
SELECT 
    Model,
    COUNT(*) AS TotalCalls,
    SUM(PromptTokens) AS TotalPromptTokens,
    SUM(CompletionTokens) AS TotalCompletionTokens,
    SUM(TotalTokens) AS TotalTokens,
    SUM(EstimatedCostUSD) AS TotalCostUSD,
    AVG(EstimatedCostUSD) AS AvgCostPerCall,
    MIN(EstimatedCostUSD) AS MinCost,
    MAX(EstimatedCostUSD) AS MaxCost
FROM OpenAIApiLogs
WHERE Model IS NOT NULL 
    AND IsSuccess = 1
GROUP BY Model
ORDER BY TotalCostUSD DESC;

GO
