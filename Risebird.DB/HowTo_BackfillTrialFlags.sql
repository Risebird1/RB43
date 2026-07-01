-- Trial flag backfill — deploy scripts + indexes first, then run candidate SP.
PRINT '=== 1. Required objects ===';
SELECT name AS MissingObject
FROM (VALUES
    ('usp_GetTrialFlagBackfillCandidates'),
    ('usp_GetTrialJudgmentContext'),
    ('usp_InsertTrialFlag')
) AS o(name)
WHERE OBJECT_ID(N'dbo.' + o.name, N'P') IS NULL;

PRINT '=== 2. Performance indexes (run Tables/AutoQcBackfillPerformanceIndexes.sql if any missing) ===';
SELECT i.name AS IndexName, OBJECT_NAME(i.object_id) AS TableName
FROM sys.indexes i
WHERE i.name IN (
    N'IX_QcInterviewAIevaluation_QCDone_QCDoneOn',
    N'IX_TrialFlags_TriggeringInterviewId',
    N'IX_TransInterviewer_IRFId_UserId',
    N'IX_InterviewMeetings_IRFId_interviewer_date'
)
ORDER BY TableName, IndexName;

PRINT '=== 3. Eligible backfill candidates (should finish in seconds) ===';
IF OBJECT_ID(N'dbo.usp_GetTrialFlagBackfillCandidates', N'P') IS NOT NULL
    EXEC dbo.usp_GetTrialFlagBackfillCandidates @MaxRows = 20, @ScanRows = 400;
ELSE
    PRINT 'Deploy StoredProcedures/usp_GetTrialFlagBackfillCandidates.sql';

PRINT '=== 4. Run backfill from /Dashboard/TrialFlagReview (or GET /Dashboard/BackfillTrialFlags) ===';
