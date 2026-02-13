-- =====================================================
-- CLEANUP: Remove Duplicate Transcript Records
-- =====================================================
-- This script removes duplicate entries from TransCriptsConv
-- Keeps the earliest record for each duplicate group
-- =====================================================

-- =====================================================
-- STEP 1: BACKUP (RECOMMENDED BEFORE RUNNING)
-- =====================================================
/*
-- Uncomment and run if you want to backup before cleanup
SELECT * INTO TransCriptsConv_Backup_20260130 FROM TransCriptsConv;
PRINT 'Backup completed successfully';
*/

-- =====================================================
-- STEP 2: IDENTIFY DUPLICATES
-- =====================================================

PRINT '========================================='
PRINT 'STEP 2a: Identifying interview question duplicates'
PRINT '========================================='
PRINT ''
PRINT 'For interview questions (DeliveryHeadQuestionId provided):'
PRINT 'Duplicate = Same SessionId + Role + DeliveryHeadQuestionId'
PRINT ''

-- Find duplicate interview question transcripts (same SessionId + Role + QuestionId)
SELECT 
    SessionId,
    Role,
    DeliveryHeadQuestionId,
    COUNT(*) AS DuplicateCount,
    MIN(Id) AS FirstId,
    MAX(Id) AS LastId,
    MIN(Timestamp) AS FirstTimestamp,
    MAX(Timestamp) AS LastTimestamp
FROM TransCriptsConv
WHERE DeliveryHeadQuestionId IS NOT NULL
GROUP BY SessionId, Role, DeliveryHeadQuestionId
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, SessionId, DeliveryHeadQuestionId, Role;

DECLARE @InterviewDuplicateCount INT;

SELECT @InterviewDuplicateCount = COUNT(*)
FROM (
    SELECT SessionId, Role, DeliveryHeadQuestionId
    FROM TransCriptsConv
    WHERE DeliveryHeadQuestionId IS NOT NULL
    GROUP BY SessionId, Role, DeliveryHeadQuestionId
    HAVING COUNT(*) > 1
) AS Dups;

PRINT ''
PRINT 'Found ' + CAST(ISNULL(@InterviewDuplicateCount, 0) AS VARCHAR(10)) + ' groups with duplicate interview question transcripts';
PRINT ''

PRINT '========================================='
PRINT 'STEP 2b: Identifying general conversation duplicates'
PRINT '========================================='
PRINT ''
PRINT 'For general conversation (no DeliveryHeadQuestionId):'
PRINT 'Duplicate = Same SessionId + Role + Message within 5 seconds'
PRINT ''

-- Find duplicate general conversation transcripts (same SessionId + Role + Message within 5 seconds)
WITH GeneralDuplicates AS (
    SELECT 
        t1.Id AS Id1,
        t1.SessionId,
        t1.Role,
        t1.Message,
        t1.Timestamp AS Timestamp1,
        t2.Id AS Id2,
        t2.Timestamp AS Timestamp2,
        DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) AS SecondsDiff
    FROM TransCriptsConv t1
    INNER JOIN TransCriptsConv t2
        ON t1.SessionId = t2.SessionId
        AND t1.Role = t2.Role
        AND t1.Message = t2.Message
        AND t1.DeliveryHeadQuestionId IS NULL
        AND t2.DeliveryHeadQuestionId IS NULL
        AND t1.Id < t2.Id
        AND DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) BETWEEN 0 AND 5
)
SELECT 
    SessionId,
    Role,
    LEFT(Message, 50) + '...' AS MessagePreview,
    COUNT(*) AS DuplicateCount,
    MIN(Id1) AS FirstId,
    MAX(Id2) AS LastId
FROM GeneralDuplicates
GROUP BY SessionId, Role, Message
ORDER BY COUNT(*) DESC, SessionId, Role;

DECLARE @GeneralDuplicateCount INT;

SELECT @GeneralDuplicateCount = COUNT(DISTINCT t2.Id)
FROM TransCriptsConv t1
INNER JOIN TransCriptsConv t2
    ON t1.SessionId = t2.SessionId
    AND t1.Role = t2.Role
    AND t1.Message = t2.Message
    AND t1.DeliveryHeadQuestionId IS NULL
    AND t2.DeliveryHeadQuestionId IS NULL
    AND t1.Id < t2.Id
    AND DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) BETWEEN 0 AND 5;

PRINT ''
PRINT 'Found approximately ' + CAST(ISNULL(@GeneralDuplicateCount, 0) AS VARCHAR(10)) + ' duplicate general conversation records';
PRINT ''

-- =====================================================
-- STEP 3: DETAILED DUPLICATE ANALYSIS
-- =====================================================

PRINT '========================================='
PRINT 'STEP 3: Detailed duplicate analysis'
PRINT '========================================='
PRINT ''

-- Show sample duplicates with full details
SELECT TOP 20
    t1.Id AS OriginalId,
    t2.Id AS DuplicateId,
    t1.SessionId,
    t1.Role,
    LEFT(t1.Message, 100) AS MessagePreview,
    t1.DeliveryHeadQuestionId,
    t1.Timestamp AS OriginalTimestamp,
    t2.Timestamp AS DuplicateTimestamp,
    DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) AS SecondsDifference
FROM TransCriptsConv t1
INNER JOIN TransCriptsConv t2
    ON t1.SessionId = t2.SessionId
    AND t1.Role = t2.Role
    AND t1.Message = t2.Message
    AND (t1.DeliveryHeadQuestionId = t2.DeliveryHeadQuestionId OR (t1.DeliveryHeadQuestionId IS NULL AND t2.DeliveryHeadQuestionId IS NULL))
    AND t1.Id < t2.Id
    AND DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) BETWEEN 0 AND 5
ORDER BY t1.SessionId, t1.Timestamp;

PRINT ''
PRINT '(Showing top 20 duplicate pairs)'
PRINT ''

-- =====================================================
-- STEP 4: REMOVE DUPLICATES (KEEPS EARLIEST)
-- =====================================================

PRINT '========================================='
PRINT 'STEP 4: Ready to remove duplicates'
PRINT '========================================='
PRINT ''
PRINT 'WARNING: The following DELETE operations will permanently remove duplicate records.'
PRINT 'Only the EARLIEST record per duplicate group will be kept.'
PRINT ''
PRINT 'Uncomment the DELETE statements below to proceed with cleanup.'
PRINT ''

-- =====================================================
-- STEP 4a: Remove duplicate interview question transcripts
-- =====================================================

/*
-- Delete duplicate interview question transcripts (keep earliest)
-- Duplicate = Same SessionId + Role + DeliveryHeadQuestionId
WITH RankedInterviewTranscripts AS (
    SELECT 
        Id,
        SessionId,
        Role,
        DeliveryHeadQuestionId,
        Timestamp,
        ROW_NUMBER() OVER (
            PARTITION BY SessionId, Role, DeliveryHeadQuestionId 
            ORDER BY Timestamp ASC, Id ASC
        ) AS RowNum
    FROM TransCriptsConv
    WHERE DeliveryHeadQuestionId IS NOT NULL
)
DELETE FROM TransCriptsConv
WHERE Id IN (
    SELECT Id 
    FROM RankedInterviewTranscripts 
    WHERE RowNum > 1
);

PRINT 'Duplicate interview question transcripts removed. Rows affected: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
*/

-- =====================================================
-- STEP 4b: Remove duplicate general conversation transcripts
-- =====================================================

/*
-- Delete duplicate general conversation transcripts (keep earliest within 5s window)
-- Duplicate = Same SessionId + Role + Message within 5 seconds
DELETE t2
FROM TransCriptsConv t1
INNER JOIN TransCriptsConv t2
    ON t1.SessionId = t2.SessionId
    AND t1.Role = t2.Role
    AND t1.Message = t2.Message
    AND t1.DeliveryHeadQuestionId IS NULL
    AND t2.DeliveryHeadQuestionId IS NULL
    AND t1.Id < t2.Id  -- Keep the earlier record (lower Id)
    AND DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) BETWEEN 0 AND 5;

PRINT 'Duplicate general conversation transcripts removed. Rows affected: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
*/

-- =====================================================
-- STEP 5: VERIFY CLEANUP
-- =====================================================

PRINT ''
PRINT '========================================='
PRINT 'STEP 5: Verify cleanup (run after DELETE)'
PRINT '========================================='

/*
-- Check for remaining interview question duplicates
SELECT 'Interview Questions' AS Type, COUNT(*) AS RemainingDuplicates
FROM (
    SELECT SessionId, Role, DeliveryHeadQuestionId
    FROM TransCriptsConv
    WHERE DeliveryHeadQuestionId IS NOT NULL
    GROUP BY SessionId, Role, DeliveryHeadQuestionId
    HAVING COUNT(*) > 1
) AS Dups

UNION ALL

-- Check for remaining general conversation duplicates
SELECT 'General Conversation' AS Type, COUNT(DISTINCT t2.Id) AS RemainingDuplicates
FROM TransCriptsConv t1
INNER JOIN TransCriptsConv t2
    ON t1.SessionId = t2.SessionId
    AND t1.Role = t2.Role
    AND t1.Message = t2.Message
    AND t1.DeliveryHeadQuestionId IS NULL
    AND t2.DeliveryHeadQuestionId IS NULL
    AND t1.Id < t2.Id
    AND DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) BETWEEN 0 AND 5;

-- Both should return 0
PRINT 'Verification complete. Check results above.';
*/

-- =====================================================
-- STEP 6: STATISTICS
-- =====================================================

PRINT ''
PRINT '========================================='
PRINT 'STEP 6: Transcript statistics'
PRINT '========================================='

SELECT 
    'Total Transcripts' AS Metric,
    COUNT(*) AS Count
FROM TransCriptsConv

UNION ALL

SELECT 
    'Unique Sessions' AS Metric,
    COUNT(DISTINCT SessionId) AS Count
FROM TransCriptsConv

UNION ALL

SELECT 
    'User Messages' AS Metric,
    COUNT(*) AS Count
FROM TransCriptsConv
WHERE Role = 'user'

UNION ALL

SELECT 
    'Assistant Messages' AS Metric,
    COUNT(*) AS Count
FROM TransCriptsConv
WHERE Role = 'assistant';

-- =====================================================
-- USAGE INSTRUCTIONS
-- =====================================================
-- 
-- 1. First, run STEP 2 to identify duplicates
-- 2. Optional: Review STEP 3 for detailed analysis
-- 3. Optional: Create backup by uncommenting STEP 1
-- 4. Uncomment and run DELETE statement in STEP 4
-- 5. Run verification query in STEP 5
-- 6. Review statistics in STEP 6
-- 
-- =====================================================

PRINT ''
PRINT '========================================='
PRINT 'CLEANUP SCRIPT READY'
PRINT '========================================='
PRINT 'Follow the steps above to clean up duplicates.'
PRINT 'Remember to uncomment the relevant sections before executing.'
