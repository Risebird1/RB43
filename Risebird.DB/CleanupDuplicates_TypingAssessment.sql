-- =====================================================
-- CLEANUP: Remove Duplicate Typing Assessment Records
-- =====================================================
-- This script removes duplicate entries from both tables
-- Keeps the most recent record per SessionId+QuestionId
-- =====================================================

-- =====================================================
-- STEP 1: BACKUP (RECOMMENDED BEFORE RUNNING)
-- =====================================================
/*
-- Uncomment and run if you want to backup before cleanup
SELECT * INTO TypingSpeedAssessment_Backup_20260130 FROM TypingSpeedAssessment;
SELECT * INTO BotInterviewQuestionVideos_Backup_20260130 FROM BotInterviewQuestionVideos;
PRINT 'Backup completed successfully';
*/

-- =====================================================
-- STEP 2: IDENTIFY DUPLICATES
-- =====================================================

PRINT '========================================='
PRINT 'STEP 2a: Check for duplicate typing assessments'
PRINT '========================================='

-- Find duplicate typing assessments (same SessionId + QuestionId)
SELECT 
    SessionId,
    QuestionId,
    COUNT(*) AS DuplicateCount,
    MIN(id) AS OldestId,
    MAX(id) AS LatestId
FROM TypingSpeedAssessment
GROUP BY SessionId, QuestionId
HAVING COUNT(*) > 1
ORDER BY SessionId, QuestionId;

DECLARE @TypingDuplicateCount INT = (
    SELECT COUNT(*) 
    FROM (
        SELECT SessionId, QuestionId
        FROM TypingSpeedAssessment
        GROUP BY SessionId, QuestionId
        HAVING COUNT(*) > 1
    ) AS Duplicates
);

PRINT 'Found ' + CAST(@TypingDuplicateCount AS VARCHAR(10)) + ' groups with duplicate typing assessments';

PRINT ''
PRINT '========================================='
PRINT 'STEP 2b: Check for duplicate video records'
PRINT '========================================='

-- Find duplicate video records (same SessionId + DeliveryHeadQuestionId)
SELECT 
    SessionId,
    DeliveryHeadQuestionId,
    COUNT(*) AS DuplicateCount,
    MIN(id) AS OldestId,
    MAX(id) AS LatestId,
    MAX(CreatedAt) AS LatestCreatedAt
FROM BotInterviewQuestionVideos
WHERE DeliveryHeadQuestionId IS NOT NULL
GROUP BY SessionId, DeliveryHeadQuestionId
HAVING COUNT(*) > 1
ORDER BY SessionId, DeliveryHeadQuestionId;

DECLARE @VideoDuplicateCount INT = (
    SELECT COUNT(*) 
    FROM (
        SELECT SessionId, DeliveryHeadQuestionId
        FROM BotInterviewQuestionVideos
        WHERE DeliveryHeadQuestionId IS NOT NULL
        GROUP BY SessionId, DeliveryHeadQuestionId
        HAVING COUNT(*) > 1
    ) AS Duplicates
);

PRINT 'Found ' + CAST(@VideoDuplicateCount AS VARCHAR(10)) + ' groups with duplicate video records';

-- =====================================================
-- STEP 3: REMOVE DUPLICATES (KEEPS MOST RECENT)
-- =====================================================

PRINT ''
PRINT '========================================='
PRINT 'STEP 3: Ready to remove duplicates'
PRINT '========================================='
PRINT ''
PRINT 'WARNING: The following DELETE operations will permanently remove duplicate records.'
PRINT 'Only the MOST RECENT record per SessionId+QuestionId will be kept.'
PRINT ''
PRINT 'Uncomment the DELETE statements below to proceed with cleanup.'
PRINT ''

-- =====================================================
-- STEP 3a: Remove duplicate typing assessments
-- =====================================================

/*
-- Delete duplicate typing assessments (keep most recent)
WITH RankedAssessments AS (
    SELECT 
        id,
        SessionId,
        QuestionId,
        CreatedAt,
        ROW_NUMBER() OVER (PARTITION BY SessionId, QuestionId ORDER BY CreatedAt DESC) AS RowNum
    FROM TypingSpeedAssessment
)
DELETE FROM TypingSpeedAssessment
WHERE id IN (
    SELECT id 
    FROM RankedAssessments 
    WHERE RowNum > 1
);

PRINT 'Duplicate typing assessments removed. Rows affected: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
*/

-- =====================================================
-- STEP 3b: Remove duplicate video records
-- =====================================================

/*
-- Delete duplicate video records (keep most recent)
WITH RankedVideos AS (
    SELECT 
        id,
        SessionId,
        DeliveryHeadQuestionId,
        CreatedAt,
        ROW_NUMBER() OVER (PARTITION BY SessionId, DeliveryHeadQuestionId ORDER BY CreatedAt DESC) AS RowNum
    FROM BotInterviewQuestionVideos
    WHERE DeliveryHeadQuestionId IS NOT NULL
)
DELETE FROM BotInterviewQuestionVideos
WHERE id IN (
    SELECT id 
    FROM RankedVideos 
    WHERE RowNum > 1
);

PRINT 'Duplicate video records removed. Rows affected: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
*/

-- =====================================================
-- STEP 4: VERIFY CLEANUP
-- =====================================================

PRINT ''
PRINT '========================================='
PRINT 'STEP 4: Verify cleanup (run after DELETE)'
PRINT '========================================='

/*
-- Check for remaining duplicates in typing assessments
SELECT 'TypingSpeedAssessment' AS TableName, COUNT(*) AS RemainingDuplicates
FROM (
    SELECT SessionId, QuestionId
    FROM TypingSpeedAssessment
    GROUP BY SessionId, QuestionId
    HAVING COUNT(*) > 1
) AS Duplicates;

-- Check for remaining duplicates in video records
SELECT 'BotInterviewQuestionVideos' AS TableName, COUNT(*) AS RemainingDuplicates
FROM (
    SELECT SessionId, DeliveryHeadQuestionId
    FROM BotInterviewQuestionVideos
    WHERE DeliveryHeadQuestionId IS NOT NULL
    GROUP BY SessionId, DeliveryHeadQuestionId
    HAVING COUNT(*) > 1
) AS Duplicates;

-- Should both return 0 remaining duplicates
*/

-- =====================================================
-- STEP 5: ADD UNIQUE CONSTRAINTS (OPTIONAL)
-- =====================================================

PRINT ''
PRINT '========================================='
PRINT 'STEP 5: Optional - Add unique constraints'
PRINT '========================================='
PRINT ''
PRINT 'To prevent future duplicates, consider adding unique constraints.'
PRINT 'Uncomment and run the code below after cleanup:'
PRINT ''

/*
-- Add unique constraint to TypingSpeedAssessment
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_TypingSpeedAssessment_SessionQuestion' 
    AND object_id = OBJECT_ID('TypingSpeedAssessment')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_TypingSpeedAssessment_SessionQuestion
    ON TypingSpeedAssessment (SessionId, QuestionId);
    PRINT '✓ Unique constraint added to TypingSpeedAssessment';
END

-- Add unique constraint to BotInterviewQuestionVideos
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_BotInterviewQuestionVideos_SessionQuestion' 
    AND object_id = OBJECT_ID('BotInterviewQuestionVideos')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_BotInterviewQuestionVideos_SessionQuestion
    ON BotInterviewQuestionVideos (SessionId, DeliveryHeadQuestionId)
    WHERE DeliveryHeadQuestionId IS NOT NULL;
    PRINT '✓ Unique constraint added to BotInterviewQuestionVideos';
END
*/

-- =====================================================
-- USAGE INSTRUCTIONS
-- =====================================================
-- 
-- 1. First, run STEP 2 to identify duplicates
-- 2. Optional: Create backups by uncommenting STEP 1
-- 3. Uncomment and run DELETE statements in STEP 3
-- 4. Run verification queries in STEP 4
-- 5. Optional: Add unique constraints in STEP 5
-- 
-- =====================================================

PRINT ''
PRINT '========================================='
PRINT 'CLEANUP SCRIPT READY'
PRINT '========================================='
PRINT 'Follow the steps above to clean up duplicates.'
PRINT 'Remember to uncomment the relevant sections before executing.'
