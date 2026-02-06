-- =============================================
-- Monitor OpenAI Cost Savings After Optimization
-- Run these queries to measure the impact
-- =============================================

PRINT '========================================';
PRINT 'OpenAI Cost Optimization Monitoring';
PRINT '========================================';
PRINT '';

-- =============================================
-- 1. Compare Token Usage Before/After Optimization
-- =============================================
PRINT '1. Token Usage Comparison:';
PRINT '   Change the dates to compare before/after deployment';
PRINT '';

-- Week BEFORE optimization (adjust dates)
DECLARE @BeforeStart DATE = '2026-01-23';
DECLARE @BeforeEnd DATE = '2026-01-30';

-- Week AFTER optimization (adjust dates)
DECLARE @AfterStart DATE = '2026-01-31';
DECLARE @AfterEnd DATE = GETDATE();

SELECT 
    'BEFORE Optimization' as Period,
    @BeforeStart as StartDate,
    @BeforeEnd as EndDate,
    COUNT(*) as TotalRuns,
    AVG(PromptTokens) as AvgInputTokens,
    AVG(CompletionTokens) as AvgOutputTokens,
    AVG(TotalTokens) as AvgTotalTokens,
    AVG(EstimatedCostUSD) as AvgCostPerRun,
    SUM(EstimatedCostUSD) as TotalCost
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND LogDate >= @BeforeStart
    AND LogDate < @BeforeEnd

UNION ALL

SELECT 
    'AFTER Optimization' as Period,
    @AfterStart as StartDate,
    @AfterEnd as EndDate,
    COUNT(*) as TotalRuns,
    AVG(PromptTokens) as AvgInputTokens,
    AVG(CompletionTokens) as AvgOutputTokens,
    AVG(TotalTokens) as AvgTotalTokens,
    AVG(EstimatedCostUSD) as AvgCostPerRun,
    SUM(EstimatedCostUSD) as TotalCost
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND LogDate >= @AfterStart
    AND LogDate <= @AfterEnd;

-- Calculate savings
PRINT '';
PRINT 'Expected improvements:';
PRINT '- Input tokens reduced by 15-30%';
PRINT '- Output tokens reduced by 10-20%';
PRINT '- Total cost reduced by 30-45%';
PRINT '';

-- =============================================
-- 2. Daily Cost Trend
-- =============================================
PRINT '';
PRINT '2. Daily Cost Trend (Last 14 Days):';

SELECT 
    CAST(LogDate AS DATE) as Date,
    COUNT(*) as TotalRuns,
    AVG(TotalTokens) as AvgTokensPerRun,
    SUM(TotalTokens) as TotalTokens,
    AVG(EstimatedCostUSD) as AvgCostPerRun,
    SUM(EstimatedCostUSD) as DailyCost
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND LogDate >= DATEADD(day, -14, GETUTCDATE())
GROUP BY CAST(LogDate AS DATE)
ORDER BY Date DESC;

PRINT '';
PRINT 'Look for:';
PRINT '- Lower DailyCost after optimization deployment';
PRINT '- Lower AvgTokensPerRun after optimization';
PRINT '- Lower AvgCostPerRun after optimization';
PRINT '';

-- =============================================
-- 3. Validation Failures (API Calls Avoided)
-- =============================================
PRINT '';
PRINT '3. Resumes Caught by Pre-validation (API Calls Saved):';

SELECT 
    ReasonBehindNotProcessed,
    COUNT(*) as Count,
    COUNT(*) * 0.025 as EstimatedSavingsUSD  -- Assuming $0.025 per call avoided
FROM CandidateResume
WHERE IsResumeParsingFailed = 1
    AND ReasonBehindNotProcessed IS NOT NULL
    AND AddedOn >= DATEADD(day, -7, GETUTCDATE())
GROUP BY ReasonBehindNotProcessed
ORDER BY Count DESC;

PRINT '';
PRINT 'These are API calls that were AVOIDED by pre-validation!';
PRINT 'Each avoided call saves ~$0.025';
PRINT '';

-- =============================================
-- 4. Cost by Operation Type (Resume Parser)
-- =============================================
PRINT '';
PRINT '4. Resume Parser Cost Breakdown:';

SELECT 
    OperationType,
    COUNT(*) as Calls,
    AVG(TotalTokens) as AvgTokens,
    SUM(EstimatedCostUSD) as TotalCost,
    AVG(EstimatedCostUSD) as AvgCost
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND LogDate >= DATEADD(day, -7, GETUTCDATE())
GROUP BY OperationType
ORDER BY TotalCost DESC;

-- =============================================
-- 5. Token Distribution Analysis
-- =============================================
PRINT '';
PRINT '5. Token Distribution (understand your usage patterns):';

SELECT 
    TokenRange,
    COUNT(*) as Count,
    AVG(EstimatedCostUSD) as AvgCost,
    SUM(EstimatedCostUSD) as TotalCost
FROM (
    SELECT 
        CASE 
            WHEN TotalTokens < 2000 THEN '0-2K (Simple)'
            WHEN TotalTokens < 4000 THEN '2K-4K (Average)'
            WHEN TotalTokens < 6000 THEN '4K-6K (Complex)'
            WHEN TotalTokens < 8000 THEN '6K-8K (Large)'
            ELSE '8K+ (Very Large)'
        END as TokenRange,
        EstimatedCostUSD
    FROM OpenAIApiLogs
    WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
        AND LogDate >= DATEADD(day, -7, GETUTCDATE())
) as TokenBuckets
GROUP BY TokenRange
ORDER BY 
    CASE TokenRange
        WHEN '0-2K (Simple)' THEN 1
        WHEN '2K-4K (Average)' THEN 2
        WHEN '4K-6K (Complex)' THEN 3
        WHEN '6K-8K (Large)' THEN 4
        ELSE 5
    END;

PRINT '';
PRINT 'Use this to understand your resume complexity distribution';
PRINT '';

-- =============================================
-- 6. Monthly Cost Projection
-- =============================================
PRINT '';
PRINT '6. Monthly Cost Projection:';

DECLARE @DaysInMonth INT = DAY(EOMONTH(GETDATE()));
DECLARE @CurrentDay INT = DAY(GETDATE());
DECLARE @DaysElapsed INT = @CurrentDay;

SELECT 
    COUNT(*) as RunsSoFar,
    SUM(EstimatedCostUSD) as CostSoFar,
    AVG(EstimatedCostUSD) as AvgCostPerRun,
    @DaysElapsed as DaysElapsed,
    @DaysInMonth as DaysInMonth,
    -- Project monthly cost based on current rate
    CAST((SUM(EstimatedCostUSD) / @DaysElapsed) * @DaysInMonth AS DECIMAL(10,2)) as ProjectedMonthlyCost,
    -- Daily average
    CAST(SUM(EstimatedCostUSD) / @DaysElapsed AS DECIMAL(10,2)) as AvgCostPerDay
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND YEAR(LogDate) = YEAR(GETDATE())
    AND MONTH(LogDate) = MONTH(GETDATE());

PRINT '';
PRINT 'Projected monthly cost based on current spending rate';
PRINT '';

-- =============================================
-- 7. Hour-by-Hour Analysis (find peak times)
-- =============================================
PRINT '';
PRINT '7. Hourly Usage Pattern (Last 7 Days):';

SELECT 
    DATEPART(HOUR, LogDate) as Hour,
    COUNT(*) as Calls,
    SUM(EstimatedCostUSD) as Cost,
    AVG(TotalTokens) as AvgTokens
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND LogDate >= DATEADD(day, -7, GETUTCDATE())
GROUP BY DATEPART(HOUR, LogDate)
ORDER BY Hour;

PRINT '';
PRINT 'Identify peak usage hours';
PRINT '';

-- =============================================
-- 8. Success Rate Monitoring
-- =============================================
PRINT '';
PRINT '8. Success Rate & Error Analysis:';

SELECT 
    IsSuccess,
    COUNT(*) as Count,
    AVG(TotalTokens) as AvgTokens,
    SUM(EstimatedCostUSD) as TotalCost,
    AVG(ResponseTimeMs) as AvgResponseTimeMs
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND LogDate >= DATEADD(day, -7, GETUTCDATE())
GROUP BY IsSuccess;

-- Failed calls details
SELECT TOP 10
    LogDate,
    ErrorMessage,
    StatusCode,
    ResponseTimeMs
FROM OpenAIApiLogs
WHERE AssistantId = 'asst_T61hVvnYmOCmvyfKylFULdUk'
    AND IsSuccess = 0
    AND LogDate >= DATEADD(day, -7, GETUTCDATE())
ORDER BY LogDate DESC;

PRINT '';
PRINT '========================================';
PRINT 'Monitoring Complete';
PRINT '========================================';
PRINT '';
PRINT 'Key Metrics to Watch:';
PRINT '1. AvgTokensPerRun should DECREASE by 20-30%';
PRINT '2. AvgCostPerRun should DECREASE by 30-45%';
PRINT '3. DailyCost should trend DOWNWARD';
PRINT '4. Validation failures show API calls AVOIDED';
PRINT '';
PRINT 'Run this script weekly to track progress!';
