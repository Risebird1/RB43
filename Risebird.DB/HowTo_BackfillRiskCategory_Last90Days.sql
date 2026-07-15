-- Idempotent backfill: re-grade completed interviews in the last 90 days via usp_AddUpdateRiskCategory.
-- Run AFTER deploying HowTo_DeployTrialRiskDeltaFixes.sql / usp_AddUpdateRiskCategory.sql.
-- Batched to avoid long transactions; re-runnable.

SET NOCOUNT ON;

DECLARE @BatchSize INT = 200;
DECLARE @Processed INT = 0;
DECLARE @BatchCount INT = 1;

IF OBJECT_ID('tempdb..#RiskBackfillIds') IS NOT NULL DROP TABLE #RiskBackfillIds;

CREATE TABLE #RiskBackfillIds (
    InterviewId INT NOT NULL PRIMARY KEY,
    Done BIT NOT NULL DEFAULT 0
);

INSERT INTO #RiskBackfillIds (InterviewId)
SELECT DISTINCT im.id
FROM dbo.InterviewMeetings im
INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
WHERE ic.MeetingStatus IN (4, 6, 7, 8)
  AND im.interviewDate >= DATEADD(DAY, -90, GETDATE());

PRINT 'Candidates: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

WHILE EXISTS (SELECT 1 FROM #RiskBackfillIds WHERE Done = 0)
BEGIN
    DECLARE @Ids TABLE (InterviewId INT PRIMARY KEY);

    DELETE FROM @Ids;
    INSERT INTO @Ids (InterviewId)
    SELECT TOP (@BatchSize) InterviewId
    FROM #RiskBackfillIds
    WHERE Done = 0
    ORDER BY InterviewId;

    DECLARE @Id INT;
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT InterviewId FROM @Ids;
    OPEN c;
    FETCH NEXT FROM c INTO @Id;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            EXEC dbo.usp_AddUpdateRiskCategory @InterviewId = @Id;
        END TRY
        BEGIN CATCH
            PRINT 'Failed InterviewId=' + CAST(@Id AS VARCHAR(20)) + ': ' + ERROR_MESSAGE();
        END CATCH;
        FETCH NEXT FROM c INTO @Id;
    END
    CLOSE c; DEALLOCATE c;

    UPDATE b SET Done = 1
    FROM #RiskBackfillIds b
    INNER JOIN @Ids i ON i.InterviewId = b.InterviewId;

    SET @Processed = @Processed + (SELECT COUNT(*) FROM @Ids);
    PRINT 'Batch ' + CAST(@BatchCount AS VARCHAR(10)) + ' done. Processed=' + CAST(@Processed AS VARCHAR(20));
    SET @BatchCount = @BatchCount + 1;
END

PRINT '=== Post-backfill severity distribution ===';
SELECT Severity, COUNT(*) AS Cnt
FROM dbo.RiskCategory
GROUP BY Severity
ORDER BY Severity;

PRINT '=== Part 1 verification: JSON-only redo (expect > 0) ===';
IF COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL
BEGIN
    SELECT COUNT(*) AS JsonOnlyRedoCount
    FROM dbo.QcInterviewAIevaluation
    WHERE ISNULL(AIMergeRedoInterview, 0) = 0
      AND ISNULL(TRY_CAST(JSON_VALUE(MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1;
END
ELSE
    PRINT 'MergedAIQCDetails column missing — skip JSON-only redo count.';

PRINT 'Backfill complete.';
GO
