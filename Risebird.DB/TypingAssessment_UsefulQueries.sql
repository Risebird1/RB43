-- =====================================================
-- USEFUL QUERIES: Typing Speed Assessment Analysis
-- =====================================================
-- Run these queries to analyze typing assessment results
-- =====================================================

-- =====================================================
-- 1. OVERVIEW: All Recent Assessments
-- =====================================================
SELECT TOP 50
    id,
    SessionId,
    QuestionId,
    WordsPerMinute,
    AccuracyPercentage,
    TimeTakenInSeconds,
    CASE 
        WHEN IsPassed = 1 THEN 'PASSED ✓'
        WHEN IsPassed = 0 THEN 'NEEDS IMPROVEMENT'
        ELSE 'NOT EVALUATED'
    END AS Status,
    CreatedAt,
    CreatedBy AS CandidateName
FROM TypingSpeedAssessment
ORDER BY CreatedAt DESC;


-- =====================================================
-- 2. PASS RATE STATISTICS
-- =====================================================
SELECT 
    COUNT(*) AS TotalAttempts,
    SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) AS Passed,
    SUM(CASE WHEN IsPassed = 0 THEN 1 ELSE 0 END) AS Failed,
    CAST(SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PassRatePercentage,
    AVG(WordsPerMinute) AS AvgWPM,
    MIN(WordsPerMinute) AS MinWPM,
    MAX(WordsPerMinute) AS MaxWPM,
    AVG(AccuracyPercentage) AS AvgAccuracy,
    MIN(AccuracyPercentage) AS MinAccuracy,
    MAX(AccuracyPercentage) AS MaxAccuracy,
    AVG(TimeTakenInSeconds) AS AvgTimeSeconds
FROM TypingSpeedAssessment;


-- =====================================================
-- 3. TOP PERFORMERS (Highest WPM with Pass Status)
-- =====================================================
SELECT TOP 10
    SessionId,
    WordsPerMinute,
    AccuracyPercentage,
    TimeTakenInSeconds,
    CreatedAt,
    CreatedBy AS CandidateName
FROM TypingSpeedAssessment
WHERE IsPassed = 1
ORDER BY WordsPerMinute DESC, AccuracyPercentage DESC;


-- =====================================================
-- 4. CANDIDATES WHO NEED IMPROVEMENT
-- =====================================================
SELECT 
    SessionId,
    WordsPerMinute,
    PassingWPM,
    AccuracyPercentage,
    PassingAccuracyPercentage,
    CASE 
        WHEN WordsPerMinute < PassingWPM AND AccuracyPercentage < PassingAccuracyPercentage 
            THEN 'Both WPM and Accuracy Below Required'
        WHEN WordsPerMinute < PassingWPM 
            THEN 'WPM Below Required (Accuracy OK)'
        WHEN AccuracyPercentage < PassingAccuracyPercentage 
            THEN 'Accuracy Below Required (WPM OK)'
        ELSE 'Unknown'
    END AS FailureReason,
    TimeTakenInSeconds,
    CreatedAt,
    CreatedBy AS CandidateName
FROM TypingSpeedAssessment
WHERE IsPassed = 0
ORDER BY CreatedAt DESC;


-- =====================================================
-- 5. PERFORMANCE BY DATE (Daily Statistics)
-- =====================================================
SELECT 
    CAST(CreatedAt AS DATE) AS AssessmentDate,
    COUNT(*) AS TotalAttempts,
    SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) AS Passed,
    SUM(CASE WHEN IsPassed = 0 THEN 1 ELSE 0 END) AS Failed,
    CAST(SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PassRate,
    AVG(WordsPerMinute) AS AvgWPM,
    AVG(AccuracyPercentage) AS AvgAccuracy
FROM TypingSpeedAssessment
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY AssessmentDate DESC;


-- =====================================================
-- 6. SPECIFIC SESSION DETAILS
-- =====================================================
-- Replace 'YOUR_SESSION_ID' with actual session ID
DECLARE @SessionId VARCHAR(50) = 'YOUR_SESSION_ID';

SELECT 
    id,
    SessionId,
    QuestionId,
    LEFT(SampleText, 100) + '...' AS SampleTextPreview,
    LEFT(TypedText, 100) + '...' AS TypedTextPreview,
    TimeStarted,
    TimeCompleted,
    TimeTakenInSeconds,
    TotalCharacters,
    CorrectCharacters,
    AccuracyPercentage,
    WordsPerMinute,
    PassingWPM,
    PassingAccuracyPercentage,
    IsPassed,
    CASE 
        WHEN IsPassed = 1 THEN 'PASSED ✓'
        ELSE 'NEEDS IMPROVEMENT'
    END AS Status,
    CreatedAt,
    CreatedBy
FROM TypingSpeedAssessment
WHERE SessionId = @SessionId;


-- =====================================================
-- 7. CANDIDATES WITH MULTIPLE ATTEMPTS
-- =====================================================
SELECT 
    SessionId,
    COUNT(*) AS AttemptCount,
    MAX(WordsPerMinute) AS BestWPM,
    MAX(AccuracyPercentage) AS BestAccuracy,
    SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) AS PassedAttempts,
    MAX(CreatedAt) AS LastAttemptDate
FROM TypingSpeedAssessment
GROUP BY SessionId
HAVING COUNT(*) > 1
ORDER BY AttemptCount DESC, BestWPM DESC;


-- =====================================================
-- 8. DISTRIBUTION: WPM RANGES
-- =====================================================
SELECT 
    CASE 
        WHEN WordsPerMinute < 20 THEN '0-19 WPM (Very Slow)'
        WHEN WordsPerMinute < 30 THEN '20-29 WPM (Below Passing)'
        WHEN WordsPerMinute < 40 THEN '30-39 WPM (Passing Range)'
        WHEN WordsPerMinute < 50 THEN '40-49 WPM (Good)'
        WHEN WordsPerMinute < 60 THEN '50-59 WPM (Very Good)'
        ELSE '60+ WPM (Excellent)'
    END AS WPMRange,
    COUNT(*) AS Count,
    CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM TypingSpeedAssessment) AS DECIMAL(5,2)) AS Percentage
FROM TypingSpeedAssessment
GROUP BY 
    CASE 
        WHEN WordsPerMinute < 20 THEN '0-19 WPM (Very Slow)'
        WHEN WordsPerMinute < 30 THEN '20-29 WPM (Below Passing)'
        WHEN WordsPerMinute < 40 THEN '30-39 WPM (Passing Range)'
        WHEN WordsPerMinute < 50 THEN '40-49 WPM (Good)'
        WHEN WordsPerMinute < 60 THEN '50-59 WPM (Very Good)'
        ELSE '60+ WPM (Excellent)'
    END
ORDER BY MIN(WordsPerMinute);


-- =====================================================
-- 9. DISTRIBUTION: Accuracy RANGES
-- =====================================================
SELECT 
    CASE 
        WHEN AccuracyPercentage < 90 THEN '0-89% (Poor)'
        WHEN AccuracyPercentage < 95 THEN '90-94% (Fair)'
        WHEN AccuracyPercentage < 97 THEN '95-96% (Below Passing)'
        WHEN AccuracyPercentage < 99 THEN '97-98% (Passing Range)'
        ELSE '99-100% (Excellent)'
    END AS AccuracyRange,
    COUNT(*) AS Count,
    CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM TypingSpeedAssessment) AS DECIMAL(5,2)) AS Percentage
FROM TypingSpeedAssessment
GROUP BY 
    CASE 
        WHEN AccuracyPercentage < 90 THEN '0-89% (Poor)'
        WHEN AccuracyPercentage < 95 THEN '90-94% (Fair)'
        WHEN AccuracyPercentage < 97 THEN '95-96% (Below Passing)'
        WHEN AccuracyPercentage < 99 THEN '97-98% (Passing Range)'
        ELSE '99-100% (Excellent)'
    END
ORDER BY MIN(AccuracyPercentage);


-- =====================================================
-- 10. TIME UTILIZATION ANALYSIS
-- =====================================================
SELECT 
    CASE 
        WHEN TimeTakenInSeconds < 60 THEN '0-59 seconds (< 1 min)'
        WHEN TimeTakenInSeconds < 90 THEN '60-89 seconds (1-1.5 min)'
        WHEN TimeTakenInSeconds < 120 THEN '90-119 seconds (1.5-2 min)'
        ELSE '120 seconds (Full 2 min)'
    END AS TimeRange,
    COUNT(*) AS Count,
    AVG(WordsPerMinute) AS AvgWPM,
    AVG(AccuracyPercentage) AS AvgAccuracy,
    SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) AS PassedCount
FROM TypingSpeedAssessment
GROUP BY 
    CASE 
        WHEN TimeTakenInSeconds < 60 THEN '0-59 seconds (< 1 min)'
        WHEN TimeTakenInSeconds < 90 THEN '60-89 seconds (1-1.5 min)'
        WHEN TimeTakenInSeconds < 120 THEN '90-119 seconds (1.5-2 min)'
        ELSE '120 seconds (Full 2 min)'
    END
ORDER BY MIN(TimeTakenInSeconds);


-- =====================================================
-- 11. CANDIDATES WHO JUST MISSED PASSING
-- =====================================================
-- Those who were close but didn't pass (WPM 28-29.99 OR Accuracy 95-96.99)
SELECT 
    SessionId,
    WordsPerMinute,
    AccuracyPercentage,
    CASE 
        WHEN WordsPerMinute BETWEEN 28 AND 29.99 THEN 'Close on WPM'
        WHEN AccuracyPercentage BETWEEN 95 AND 96.99 THEN 'Close on Accuracy'
        ELSE 'Close on Both'
    END AS CloseReason,
    TimeTakenInSeconds,
    CreatedAt,
    CreatedBy AS CandidateName
FROM TypingSpeedAssessment
WHERE IsPassed = 0
  AND (WordsPerMinute BETWEEN 28 AND 29.99 OR AccuracyPercentage BETWEEN 95 AND 96.99)
ORDER BY WordsPerMinute DESC, AccuracyPercentage DESC;


-- =====================================================
-- 12. EXPORT DATA FOR ANALYSIS (CSV-friendly format)
-- =====================================================
SELECT 
    SessionId AS 'Session ID',
    QuestionId AS 'Question ID',
    CAST(TimeStarted AS VARCHAR(23)) AS 'Start Time',
    CAST(TimeCompleted AS VARCHAR(23)) AS 'End Time',
    TimeTakenInSeconds AS 'Time Taken (seconds)',
    TotalCharacters AS 'Total Characters',
    CorrectCharacters AS 'Correct Characters',
    AccuracyPercentage AS 'Accuracy %',
    WordsPerMinute AS 'WPM',
    PassingWPM AS 'Required WPM',
    PassingAccuracyPercentage AS 'Required Accuracy %',
    CASE WHEN IsPassed = 1 THEN 'PASSED' ELSE 'FAILED' END AS 'Result',
    CAST(CreatedAt AS VARCHAR(23)) AS 'Assessment Date',
    CreatedBy AS 'Candidate Name'
FROM TypingSpeedAssessment
ORDER BY CreatedAt DESC;


-- =====================================================
-- 13. RETROACTIVE UPDATE (Run once if needed)
-- =====================================================
-- Use this to update existing records that don't have IsPassed calculated
/*
UPDATE TypingSpeedAssessment
SET 
    PassingWPM = 30.00,
    PassingAccuracyPercentage = 97.00,
    IsPassed = CASE 
        WHEN WordsPerMinute >= 30.00 AND AccuracyPercentage >= 97.00 
        THEN 1 
        ELSE 0 
    END
WHERE IsPassed IS NULL;

SELECT @@ROWCOUNT AS 'Records Updated';
*/


-- =====================================================
-- 14. SAMPLE TEXT USAGE ANALYSIS
-- =====================================================
-- See which sample texts are used most frequently
SELECT 
    LEFT(SampleText, 80) + '...' AS SampleTextPreview,
    COUNT(*) AS UsageCount,
    AVG(WordsPerMinute) AS AvgWPM,
    AVG(AccuracyPercentage) AS AvgAccuracy,
    SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) AS PassCount,
    CAST(SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PassRate
FROM TypingSpeedAssessment
GROUP BY LEFT(SampleText, 80)
HAVING COUNT(*) > 1
ORDER BY UsageCount DESC;


-- =====================================================
-- 15. MONTHLY TREND ANALYSIS
-- =====================================================
SELECT 
    YEAR(CreatedAt) AS Year,
    MONTH(CreatedAt) AS Month,
    DATENAME(MONTH, CreatedAt) AS MonthName,
    COUNT(*) AS TotalAttempts,
    SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) AS Passed,
    CAST(SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PassRate,
    AVG(WordsPerMinute) AS AvgWPM,
    AVG(AccuracyPercentage) AS AvgAccuracy
FROM TypingSpeedAssessment
GROUP BY YEAR(CreatedAt), MONTH(CreatedAt), DATENAME(MONTH, CreatedAt)
ORDER BY Year DESC, Month DESC;


-- =====================================================
-- END OF QUERIES
-- =====================================================
-- For more information, see: TYPING_ASSESSMENT_SCORING_UPDATE.md
-- =====================================================
