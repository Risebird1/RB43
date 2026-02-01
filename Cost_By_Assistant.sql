-- =============================================
-- Find Which AssistantId Cost the Most
-- Based on estimation methodology
-- =============================================

SET NOCOUNT ON;

PRINT '========================================';
PRINT 'COST BY ASSISTANT ID (ESTIMATED)';
PRINT '========================================';
PRINT '';

-- Get cost estimates by AssistantId
SELECT 
    ISNULL(AssistantId, 'Unknown/NULL') as AssistantId,
    COUNT(DISTINCT RunId) as CompletedRuns,
    COUNT(*) as TotalAPICalls,
    
    -- Count by operation type
    SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 1 ELSE 0 END) as CandidateScreenings,
    SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 1 ELSE 0 END) as InterviewerScreenings,
    SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 1 ELSE 0 END) as ResumeAnalysis,
    SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 1 ELSE 0 END) as ReportQC,
    SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 1 ELSE 0 END) as AIInterviews,
    
    -- Estimated costs (Moderate scenario)
    CAST(
        SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END) +
        SUM(CASE WHEN OperationType NOT LIKE '%Candidate%' 
                  AND OperationType NOT LIKE '%Interviewer%Screen%' 
                  AND OperationType NOT LIKE '%Resume%' 
                  AND OperationType NOT LIKE '%QC%' 
                  AND OperationType NOT LIKE '%Report%' 
                  AND OperationType NOT LIKE '%AIInterview%' 
                  AND RunId IS NOT NULL THEN 0.025 ELSE 0 END)
    AS DECIMAL(10,2)) as EstimatedCostUSD_Moderate,
    
    MIN(LogDate) as FirstUsed,
    MAX(LogDate) as LastUsed,
    DATEDIFF(day, MIN(LogDate), MAX(LogDate)) + 1 as DaysActive
    
FROM OpenAIApiLogs
WHERE IsSuccess = 1
    AND PromptTokens = 0  -- Historical data without token info
    AND (RunId IS NOT NULL OR ApiEndpoint LIKE '%/assistants/%')
GROUP BY AssistantId
ORDER BY EstimatedCostUSD_Moderate DESC;

PRINT '';
PRINT '========================================';
PRINT 'TOP 5 MOST EXPENSIVE ASSISTANTS';
PRINT '========================================';
PRINT '';

-- Detailed breakdown of top 5
SELECT TOP 5
    AssistantId,
    CompletedRuns,
    EstimatedCostUSD_Moderate as [Est. Cost],
    DaysActive,
    CAST(EstimatedCostUSD_Moderate / NULLIF(DaysActive, 0) AS DECIMAL(10,2)) as [Cost Per Day]
FROM (
    SELECT 
        ISNULL(AssistantId, 'Unknown') as AssistantId,
        COUNT(DISTINCT RunId) as CompletedRuns,
        CAST(
            SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
            SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
            SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
            SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
            SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END) +
            SUM(CASE WHEN OperationType NOT LIKE '%Candidate%' 
                      AND OperationType NOT LIKE '%Interviewer%Screen%' 
                      AND OperationType NOT LIKE '%Resume%' 
                      AND OperationType NOT LIKE '%QC%' 
                      AND OperationType NOT LIKE '%Report%' 
                      AND OperationType NOT LIKE '%AIInterview%' 
                      AND RunId IS NOT NULL THEN 0.025 ELSE 0 END)
        AS DECIMAL(10,2)) as EstimatedCostUSD_Moderate,
        DATEDIFF(day, MIN(LogDate), MAX(LogDate)) + 1 as DaysActive
    FROM OpenAIApiLogs
    WHERE IsSuccess = 1
        AND PromptTokens = 0
        AND (RunId IS NOT NULL OR ApiEndpoint LIKE '%/assistants/%')
    GROUP BY AssistantId
) sub
ORDER BY EstimatedCostUSD_Moderate DESC;

PRINT '';
PRINT '========================================';
PRINT 'ASSISTANT USAGE BREAKDOWN';
PRINT '========================================';
PRINT '';

-- Detailed breakdown for each assistant
SELECT 
    ISNULL(AssistantId, 'Unknown') as AssistantId,
    
    -- Operation counts
    SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 1 ELSE 0 END) as [Candidate Screenings],
    SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 1 ELSE 0 END) as [Interviewer Screenings],
    SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 1 ELSE 0 END) as [Resume Analysis],
    SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 1 ELSE 0 END) as [Report QC],
    SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 1 ELSE 0 END) as [AI Interviews],
    
    -- Estimated costs per operation type
    '$' + CAST(CAST(SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) AS DECIMAL(10,2)) AS VARCHAR) as [Cost: Candidates],
    '$' + CAST(CAST(SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) AS DECIMAL(10,2)) AS VARCHAR) as [Cost: Interviewers],
    '$' + CAST(CAST(SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) AS DECIMAL(10,2)) AS VARCHAR) as [Cost: Resumes],
    '$' + CAST(CAST(SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) AS DECIMAL(10,2)) AS VARCHAR) as [Cost: QC],
    '$' + CAST(CAST(SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END) AS DECIMAL(10,2)) AS VARCHAR) as [Cost: AI Interviews],
    
    -- Total
    '$' + CAST(CAST(
        SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END)
    AS DECIMAL(10,2)) AS VARCHAR) as [TOTAL COST (Est)]
    
FROM OpenAIApiLogs
WHERE IsSuccess = 1
    AND PromptTokens = 0
    AND RunId IS NOT NULL
GROUP BY AssistantId
HAVING COUNT(DISTINCT RunId) > 0
ORDER BY 
    CAST(
        SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END)
    AS DECIMAL(10,2)) DESC;

PRINT '';
PRINT '========================================';
PRINT 'ASSISTANT CONFIGURATION LOOKUP';
PRINT '========================================';
PRINT '';
PRINT 'Use this to map AssistantIds to their purpose:';
PRINT '';

-- Try to identify assistant purpose from operation patterns
SELECT 
    AssistantId,
    
    -- Identify primary purpose
    CASE 
        WHEN SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 1 ELSE 0 END) > 
             SUM(CASE WHEN OperationType NOT LIKE '%Candidate%' THEN 1 ELSE 0 END) 
            THEN 'Candidate Screening Assistant'
        WHEN SUM(CASE WHEN OperationType LIKE '%Interviewer%' THEN 1 ELSE 0 END) > 
             SUM(CASE WHEN OperationType NOT LIKE '%Interviewer%' THEN 1 ELSE 0 END) 
            THEN 'Interviewer Screening Assistant'
        WHEN SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 1 ELSE 0 END) > 
             SUM(CASE WHEN OperationType NOT LIKE '%Resume%' THEN 1 ELSE 0 END) 
            THEN 'Resume Parser Assistant'
        WHEN SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 1 ELSE 0 END) > 
             SUM(CASE WHEN OperationType NOT LIKE '%QC%' AND OperationType NOT LIKE '%Report%' THEN 1 ELSE 0 END) 
            THEN 'Report QC Assistant'
        WHEN SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 1 ELSE 0 END) > 
             SUM(CASE WHEN OperationType NOT LIKE '%AIInterview%' THEN 1 ELSE 0 END) 
            THEN 'AI Interviewer Assistant'
        ELSE 'Mixed Purpose Assistant'
    END as [Likely Purpose],
    
    COUNT(DISTINCT RunId) as [Total Runs],
    
    '$' + CAST(CAST(
        SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END)
    AS DECIMAL(10,2)) AS VARCHAR) as [Est. Cost]
    
FROM OpenAIApiLogs
WHERE IsSuccess = 1
    AND PromptTokens = 0
    AND RunId IS NOT NULL
    AND AssistantId IS NOT NULL
GROUP BY AssistantId
ORDER BY 
    CAST(
        SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END)
    AS DECIMAL(10,2)) DESC;

PRINT '';
PRINT '========================================';
PRINT 'SUMMARY & RECOMMENDATIONS';
PRINT '========================================';
PRINT '';

-- Summary statistics
DECLARE @TotalAssistants INT;
DECLARE @MostExpensiveAssistant NVARCHAR(100);
DECLARE @MostExpensiveCost DECIMAL(10,2);
DECLARE @TotalEstimatedCost DECIMAL(10,2);

SELECT 
    @TotalAssistants = COUNT(DISTINCT AssistantId),
    @TotalEstimatedCost = SUM(EstCost)
FROM (
    SELECT 
        AssistantId,
        SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END) as EstCost
    FROM OpenAIApiLogs
    WHERE IsSuccess = 1 AND PromptTokens = 0 AND RunId IS NOT NULL
    GROUP BY AssistantId
) sub;

SELECT TOP 1
    @MostExpensiveAssistant = ISNULL(AssistantId, 'Unknown'),
    @MostExpensiveCost = CAST(
        SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
        SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END)
    AS DECIMAL(10,2))
FROM OpenAIApiLogs
WHERE IsSuccess = 1 AND PromptTokens = 0 AND RunId IS NOT NULL
GROUP BY AssistantId
ORDER BY SUM(CASE WHEN OperationType LIKE '%Candidate%' THEN 0.035 ELSE 0 END) +
         SUM(CASE WHEN OperationType LIKE '%Interviewer%Screen%' THEN 0.030 ELSE 0 END) +
         SUM(CASE WHEN OperationType LIKE '%Resume%' THEN 0.045 ELSE 0 END) +
         SUM(CASE WHEN OperationType LIKE '%QC%' OR OperationType LIKE '%Report%' THEN 0.055 ELSE 0 END) +
         SUM(CASE WHEN OperationType LIKE '%AIInterview%' THEN 0.125 ELSE 0 END) DESC;

PRINT 'Total Unique Assistants: ' + CAST(@TotalAssistants AS VARCHAR);
PRINT 'Total Estimated Cost: $' + CAST(@TotalEstimatedCost AS VARCHAR);
PRINT '';
PRINT '🏆 MOST EXPENSIVE ASSISTANT:';
PRINT '   AssistantId: ' + @MostExpensiveAssistant;
PRINT '   Estimated Cost: $' + CAST(@MostExpensiveCost AS VARCHAR);
PRINT '   % of Total: ' + CAST(CAST((@MostExpensiveCost / NULLIF(@TotalEstimatedCost, 0) * 100) AS DECIMAL(5,1)) AS VARCHAR) + '%';
PRINT '';
PRINT '💡 RECOMMENDATIONS:';
PRINT '1. Review the most expensive assistant configuration';
PRINT '2. Check if prompts can be optimized to reduce tokens';
PRINT '3. Consider if cheaper models (gpt-4o-mini) could work for simpler tasks';
PRINT '4. Monitor usage patterns after deploying the logging fix';
PRINT '';

SET NOCOUNT OFF;
