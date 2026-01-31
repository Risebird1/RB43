-- =============================================
-- Check Historical OpenAI Logs Data
-- This will show why recalculation didn't work
-- =============================================

PRINT '=== CHECKING HISTORICAL DATA ===';
PRINT '';

-- Check if we have any token data at all
PRINT '1. Token Data Availability:';
SELECT 
    COUNT(*) as TotalLogs,
    SUM(CASE WHEN PromptTokens > 0 OR CompletionTokens > 0 THEN 1 ELSE 0 END) as LogsWithTokens,
    SUM(CASE WHEN PromptTokens = 0 AND CompletionTokens = 0 THEN 1 ELSE 0 END) as LogsWithoutTokens,
    SUM(CASE WHEN EstimatedCostUSD > 0 THEN 1 ELSE 0 END) as LogsWithCost
FROM OpenAIApiLogs;

PRINT '';
PRINT '2. Breakdown by Operation Type:';
SELECT 
    OperationType,
    COUNT(*) as TotalCalls,
    SUM(CASE WHEN PromptTokens > 0 THEN 1 ELSE 0 END) as CallsWithTokens,
    SUM(TotalTokens) as TotalTokensSum,
    SUM(EstimatedCostUSD) as TotalCost
FROM OpenAIApiLogs
GROUP BY OperationType
ORDER BY TotalCalls DESC;

PRINT '';
PRINT '3. Sample of logs (most recent):';
SELECT TOP 10
    Id,
    LogDate,
    ApiEndpoint,
    OperationType,
    Model,
    PromptTokens,
    CompletionTokens,
    TotalTokens,
    EstimatedCostUSD,
    IsSuccess
FROM OpenAIApiLogs
ORDER BY LogDate DESC;

PRINT '';
PRINT '4. Check for Whisper logs (should have costs):';
SELECT 
    COUNT(*) as WhisperLogs,
    SUM(EstimatedCostUSD) as TotalWhisperCost
FROM OpenAIApiLogs
WHERE ApiEndpoint LIKE '%/audio/transcriptions%'
    OR Model LIKE '%whisper%';

PRINT '';
PRINT '=== DIAGNOSIS ===';
PRINT 'If "LogsWithTokens" = 0 for Assistants API calls, then historical data CANNOT be recalculated.';
PRINT 'The token data was never captured, so there is nothing to calculate costs from.';
PRINT '';
PRINT 'SOLUTION: Deploy the code fix, then NEW calls will have correct token data.';
PRINT 'Historical data is unfortunately lost and cannot be recovered.';
