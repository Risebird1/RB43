-- =============================================
-- Estimate Historical OpenAI Costs
-- Based on API call patterns and industry averages
-- =============================================

SET NOCOUNT ON;

PRINT '========================================';
PRINT 'OpenAI Historical Cost Estimation';
PRINT '========================================';
PRINT '';

-- Create temp table for estimations
IF OBJECT_ID('tempdb..#CostEstimates') IS NOT NULL DROP TABLE #CostEstimates;
CREATE TABLE #CostEstimates (
    EstimationMethod NVARCHAR(100),
    EstimatedCost DECIMAL(18,2),
    Confidence NVARCHAR(20),
    Notes NVARCHAR(500)
);

-- =============================================
-- STEP 1: Analyze existing data
-- =============================================
PRINT '1. ANALYZING EXISTING DATA';
PRINT '-------------------------------------------';

DECLARE @TotalLogs INT;
DECLARE @LogsWithTokens INT;
DECLARE @LogsWithoutTokens INT;
DECLARE @WhisperCost DECIMAL(18,2);
DECLARE @TTSCost DECIMAL(18,2);
DECLARE @AssistantAPICalls INT;
DECLARE @DateRangeStart DATETIME;
DECLARE @DateRangeEnd DATETIME;

SELECT 
    @TotalLogs = COUNT(*),
    @LogsWithTokens = SUM(CASE WHEN PromptTokens > 0 OR CompletionTokens > 0 THEN 1 ELSE 0 END),
    @LogsWithoutTokens = SUM(CASE WHEN PromptTokens = 0 AND CompletionTokens = 0 THEN 1 ELSE 0 END),
    @WhisperCost = SUM(CASE WHEN ApiEndpoint LIKE '%/audio/transcriptions%' THEN EstimatedCostUSD ELSE 0 END),
    @TTSCost = SUM(CASE WHEN ApiEndpoint LIKE '%/audio/speech%' THEN EstimatedCostUSD ELSE 0 END),
    @DateRangeStart = MIN(LogDate),
    @DateRangeEnd = MAX(LogDate)
FROM OpenAIApiLogs;

-- Count Assistant API calls that likely consumed tokens
SELECT @AssistantAPICalls = COUNT(*)
FROM OpenAIApiLogs
WHERE (OperationType LIKE '%Run%' 
    OR ApiEndpoint LIKE '%/threads/%/runs%'
    OR ApiEndpoint LIKE '%/assistants/%')
    AND PromptTokens = 0 
    AND CompletionTokens = 0
    AND IsSuccess = 1;

PRINT 'Data Range: ' + CONVERT(VARCHAR, @DateRangeStart, 120) + ' to ' + CONVERT(VARCHAR, @DateRangeEnd, 120);
PRINT 'Total API Calls: ' + CAST(@TotalLogs AS VARCHAR);
PRINT 'Calls WITH Token Data: ' + CAST(@LogsWithTokens AS VARCHAR);
PRINT 'Calls WITHOUT Token Data: ' + CAST(@LogsWithoutTokens AS VARCHAR);
PRINT 'Assistant API Calls (no tokens): ' + CAST(@AssistantAPICalls AS VARCHAR);
PRINT 'Known Whisper Costs: $' + CAST(@WhisperCost AS VARCHAR);
PRINT 'Known TTS Costs: $' + CAST(@TTSCost AS VARCHAR);
PRINT '';

-- =============================================
-- STEP 2: Count successful completed runs
-- =============================================
PRINT '2. COUNTING COMPLETED RUNS';
PRINT '-------------------------------------------';

DECLARE @CompletedRuns INT;
DECLARE @CandidateScreenings INT;
DECLARE @InterviewerScreenings INT;
DECLARE @ResumeAnalysis INT;
DECLARE @ReportQC INT;
DECLARE @AIInterviews INT;
DECLARE @OtherOperations INT;

-- Count completed runs by operation type
SELECT 
    @CompletedRuns = COUNT(DISTINCT RunId)
FROM OpenAIApiLogs
WHERE RunId IS NOT NULL 
    AND IsSuccess = 1
    AND PromptTokens = 0;

SELECT @CandidateScreenings = COUNT(DISTINCT RunId)
FROM OpenAIApiLogs
WHERE OperationType LIKE '%Candidate%' AND RunId IS NOT NULL AND IsSuccess = 1 AND PromptTokens = 0;

SELECT @InterviewerScreenings = COUNT(DISTINCT RunId)
FROM OpenAIApiLogs
WHERE OperationType LIKE '%Interviewer%Screen%' AND RunId IS NOT NULL AND IsSuccess = 1 AND PromptTokens = 0;

SELECT @ResumeAnalysis = COUNT(DISTINCT RunId)
FROM OpenAIApiLogs
WHERE OperationType LIKE '%Resume%' AND RunId IS NOT NULL AND IsSuccess = 1 AND PromptTokens = 0;

SELECT @ReportQC = COUNT(DISTINCT RunId)
FROM OpenAIApiLogs
WHERE OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' AND RunId IS NOT NULL AND IsSuccess = 1 AND PromptTokens = 0;

SELECT @AIInterviews = COUNT(DISTINCT RunId)
FROM OpenAIApiLogs
WHERE OperationType LIKE '%AIInterview%' AND RunId IS NOT NULL AND IsSuccess = 1 AND PromptTokens = 0;

SET @OtherOperations = @CompletedRuns - (@CandidateScreenings + @InterviewerScreenings + @ResumeAnalysis + @ReportQC + @AIInterviews);

PRINT 'Completed Runs (estimated):';
PRINT '  - Candidate Screenings: ' + CAST(@CandidateScreenings AS VARCHAR);
PRINT '  - Interviewer Screenings: ' + CAST(@InterviewerScreenings AS VARCHAR);
PRINT '  - Resume Analysis: ' + CAST(@ResumeAnalysis AS VARCHAR);
PRINT '  - Report QC: ' + CAST(@ReportQC AS VARCHAR);
PRINT '  - AI Interviews: ' + CAST(@AIInterviews AS VARCHAR);
PRINT '  - Other Operations: ' + CAST(@OtherOperations AS VARCHAR);
PRINT '  - TOTAL: ' + CAST(@CompletedRuns AS VARCHAR);
PRINT '';

-- =============================================
-- STEP 3: Industry average cost estimates
-- =============================================
PRINT '3. COST ESTIMATION METHODS';
PRINT '-------------------------------------------';
PRINT '';

-- Method 1: Conservative estimate (low usage)
-- Assumes minimal tokens per operation
DECLARE @ConservativeEstimate DECIMAL(18,2);
SET @ConservativeEstimate = 
    (@CandidateScreenings * 0.015) +      -- ~$0.015 per screening (1500 tokens)
    (@InterviewerScreenings * 0.012) +    -- ~$0.012 per screening (1200 tokens)
    (@ResumeAnalysis * 0.020) +           -- ~$0.020 per analysis (2000 tokens)
    (@ReportQC * 0.025) +                 -- ~$0.025 per QC (2500 tokens)
    (@AIInterviews * 0.050) +             -- ~$0.050 per interview (5000 tokens)
    (@OtherOperations * 0.010) +          -- ~$0.010 per other operation
    @WhisperCost + @TTSCost;

INSERT INTO #CostEstimates VALUES 
('Conservative (Low Usage)', @ConservativeEstimate, 'Low', 
 'Assumes minimal token usage: 1000-2500 tokens per operation');

PRINT 'Method 1: CONSERVATIVE ESTIMATE';
PRINT '  Assumptions:';
PRINT '    - Candidate Screening: ~1500 tokens ($0.015/call)';
PRINT '    - Interviewer Screening: ~1200 tokens ($0.012/call)';
PRINT '    - Resume Analysis: ~2000 tokens ($0.020/call)';
PRINT '    - Report QC: ~2500 tokens ($0.025/call)';
PRINT '    - AI Interview: ~5000 tokens ($0.050/call)';
PRINT '  Estimated Cost: $' + CAST(@ConservativeEstimate AS VARCHAR);
PRINT '';

-- Method 2: Moderate estimate (typical usage)
DECLARE @ModerateEstimate DECIMAL(18,2);
SET @ModerateEstimate = 
    (@CandidateScreenings * 0.035) +      -- ~$0.035 per screening (3500 tokens)
    (@InterviewerScreenings * 0.030) +    -- ~$0.030 per screening (3000 tokens)
    (@ResumeAnalysis * 0.045) +           -- ~$0.045 per analysis (4500 tokens)
    (@ReportQC * 0.055) +                 -- ~$0.055 per QC (5500 tokens)
    (@AIInterviews * 0.125) +             -- ~$0.125 per interview (12500 tokens)
    (@OtherOperations * 0.025) +          -- ~$0.025 per other operation
    @WhisperCost + @TTSCost;

INSERT INTO #CostEstimates VALUES 
('Moderate (Typical Usage)', @ModerateEstimate, 'Medium', 
 'Assumes typical token usage: 3000-5500 tokens per operation');

PRINT 'Method 2: MODERATE ESTIMATE (Most Likely)';
PRINT '  Assumptions:';
PRINT '    - Candidate Screening: ~3500 tokens ($0.035/call)';
PRINT '    - Interviewer Screening: ~3000 tokens ($0.030/call)';
PRINT '    - Resume Analysis: ~4500 tokens ($0.045/call)';
PRINT '    - Report QC: ~5500 tokens ($0.055/call)';
PRINT '    - AI Interview: ~12500 tokens ($0.125/call)';
PRINT '  Estimated Cost: $' + CAST(@ModerateEstimate AS VARCHAR);
PRINT '';

-- Method 3: High estimate (heavy usage)
DECLARE @HighEstimate DECIMAL(18,2);
SET @HighEstimate = 
    (@CandidateScreenings * 0.070) +      -- ~$0.070 per screening (7000 tokens)
    (@InterviewerScreenings * 0.060) +    -- ~$0.060 per screening (6000 tokens)
    (@ResumeAnalysis * 0.090) +           -- ~$0.090 per analysis (9000 tokens)
    (@ReportQC * 0.110) +                 -- ~$0.110 per QC (11000 tokens)
    (@AIInterviews * 0.250) +             -- ~$0.250 per interview (25000 tokens)
    (@OtherOperations * 0.050) +          -- ~$0.050 per other operation
    @WhisperCost + @TTSCost;

INSERT INTO #CostEstimates VALUES 
('High (Heavy Usage)', @HighEstimate, 'High', 
 'Assumes heavy token usage: 6000-11000 tokens per operation');

PRINT 'Method 3: HIGH ESTIMATE';
PRINT '  Assumptions:';
PRINT '    - Candidate Screening: ~7000 tokens ($0.070/call)';
PRINT '    - Interviewer Screening: ~6000 tokens ($0.060/call)';
PRINT '    - Resume Analysis: ~9000 tokens ($0.090/call)';
PRINT '    - Report QC: ~11000 tokens ($0.110/call)';
PRINT '    - AI Interview: ~25000 tokens ($0.250/call)';
PRINT '  Estimated Cost: $' + CAST(@HighEstimate AS VARCHAR);
PRINT '';

-- Method 4: Per-day average estimate
DECLARE @DayCount INT = DATEDIFF(day, @DateRangeStart, @DateRangeEnd) + 1;
DECLARE @CallsPerDay DECIMAL(18,2) = CAST(@AssistantAPICalls AS DECIMAL) / @DayCount;
DECLARE @DailyEstimate DECIMAL(18,2) = @CallsPerDay * 0.035; -- Average $0.035 per call
DECLARE @TotalDailyEstimate DECIMAL(18,2) = @DailyEstimate * @DayCount;

INSERT INTO #CostEstimates VALUES 
('Daily Average Method', @TotalDailyEstimate, 'Medium', 
 CAST(@CallsPerDay AS VARCHAR(10)) + ' calls/day × $0.035 avg × ' + CAST(@DayCount AS VARCHAR) + ' days');

PRINT 'Method 4: DAILY AVERAGE';
PRINT '  Time Period: ' + CAST(@DayCount AS VARCHAR) + ' days';
PRINT '  Calls per Day: ' + CAST(@CallsPerDay AS VARCHAR);
PRINT '  Daily Cost: $' + CAST(@DailyEstimate AS VARCHAR);
PRINT '  Total Estimated Cost: $' + CAST(@TotalDailyEstimate AS VARCHAR);
PRINT '';

-- =============================================
-- STEP 4: Summary and recommendation
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'COST ESTIMATION SUMMARY';
PRINT '========================================';
PRINT '';

SELECT 
    EstimationMethod,
    '$' + CAST(EstimatedCost AS VARCHAR) as EstimatedCost,
    Confidence,
    Notes
FROM #CostEstimates
ORDER BY EstimatedCost;

PRINT '';
PRINT '========================================';
PRINT 'RECOMMENDATION';
PRINT '========================================';
PRINT '';
PRINT 'Based on analysis, your historical OpenAI costs were LIKELY:';
PRINT '';
PRINT '  MINIMUM: $' + CAST(@ConservativeEstimate AS VARCHAR);
PRINT '  LIKELY:  $' + CAST(@ModerateEstimate AS VARCHAR) + ' ← BEST ESTIMATE';
PRINT '  MAXIMUM: $' + CAST(@HighEstimate AS VARCHAR);
PRINT '';
PRINT 'Known Costs (from Whisper/TTS): $' + CAST(@WhisperCost + @TTSCost AS VARCHAR);
PRINT 'Estimated Assistants API Costs: $' + CAST(@ModerateEstimate - (@WhisperCost + @TTSCost) AS VARCHAR);
PRINT '';
PRINT '⚠️ IMPORTANT NOTES:';
PRINT '1. These are ESTIMATES based on industry averages';
PRINT '2. Actual costs depend on:';
PRINT '   - Length of resumes/documents processed';
PRINT '   - Complexity of screening criteria';
PRINT '   - Conversation length in AI interviews';
PRINT '   - Vector store searches (if used)';
PRINT '3. Range could vary ±50% from estimates';
PRINT '';

-- =============================================
-- STEP 5: Breakdown by time period
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'MONTHLY BREAKDOWN (Moderate Estimate)';
PRINT '========================================';
PRINT '';

SELECT 
    FORMAT(LogDate, 'yyyy-MM') as Month,
    COUNT(DISTINCT RunId) as CompletedRuns,
    COUNT(*) as TotalAPICalls,
    '$' + CAST(CAST(COUNT(DISTINCT RunId) * 0.035 AS DECIMAL(10,2)) AS VARCHAR) as EstimatedCost
FROM OpenAIApiLogs
WHERE RunId IS NOT NULL 
    AND IsSuccess = 1
    AND PromptTokens = 0
GROUP BY FORMAT(LogDate, 'yyyy-MM')
ORDER BY FORMAT(LogDate, 'yyyy-MM') DESC;

PRINT '';
PRINT '========================================';
PRINT 'COST BY OPERATION TYPE (Moderate)';
PRINT '========================================';
PRINT '';

SELECT 
    CASE 
        WHEN OperationType LIKE '%Candidate%' THEN 'Candidate Screening'
        WHEN OperationType LIKE '%Interviewer%Screen%' THEN 'Interviewer Screening'
        WHEN OperationType LIKE '%Resume%' THEN 'Resume Analysis'
        WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 'Report QC'
        WHEN OperationType LIKE '%AIInterview%' THEN 'AI Interview'
        ELSE 'Other'
    END as OperationCategory,
    COUNT(DISTINCT RunId) as Runs,
    '$' + CAST(CAST(
        COUNT(DISTINCT RunId) * 
        CASE 
            WHEN OperationType LIKE '%Candidate%' THEN 0.035
            WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030
            WHEN OperationType LIKE '%Resume%' THEN 0.045
            WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055
            WHEN OperationType LIKE '%AIInterview%' THEN 0.125
            ELSE 0.025
        END AS DECIMAL(10,2)
    ) AS VARCHAR) as EstimatedCost
FROM OpenAIApiLogs
WHERE RunId IS NOT NULL 
    AND IsSuccess = 1
    AND PromptTokens = 0
GROUP BY 
    CASE 
        WHEN OperationType LIKE '%Candidate%' THEN 'Candidate Screening'
        WHEN OperationType LIKE '%Interviewer%Screen%' THEN 'Interviewer Screening'
        WHEN OperationType LIKE '%Resume%' THEN 'Resume Analysis'
        WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 'Report QC'
        WHEN OperationType LIKE '%AIInterview%' THEN 'AI Interview'
        ELSE 'Other'
    END,
    CASE 
        WHEN OperationType LIKE '%Candidate%' THEN 0.035
        WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030
        WHEN OperationType LIKE '%Resume%' THEN 0.045
        WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055
        WHEN OperationType LIKE '%AIInterview%' THEN 0.125
        ELSE 0.025
    END
ORDER BY 
    CAST(COUNT(DISTINCT RunId) * 
        CASE 
            WHEN OperationType LIKE '%Candidate%' THEN 0.035
            WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030
            WHEN OperationType LIKE '%Resume%' THEN 0.045
            WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055
            WHEN OperationType LIKE '%AIInterview%' THEN 0.125
            ELSE 0.025
        END AS DECIMAL(10,2)) DESC;

PRINT '';
PRINT '========================================';
PRINT 'NEXT STEPS';
PRINT '========================================';
PRINT '';
PRINT '1. Use the MODERATE estimate for budgeting purposes';
PRINT '2. Deploy the code fix IMMEDIATELY to capture actual costs';
PRINT '3. Compare actual costs (after fix) with estimates';
PRINT '4. Adjust future budgets based on real data';
PRINT '';

-- Cleanup
DROP TABLE #CostEstimates;

SET NOCOUNT OFF;
