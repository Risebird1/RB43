/* =============================================================================
   Trial / RiskCategory delta-4 — one-time migration (idempotent / re-runnable)
   Run AFTER deploying:
     1) usp_AddUpdateRiskCategory.sql          (Part 1 — no husk rows)
     2) usp_GetTrialFlagsForReview.sql         (Part 2 — live DisplaySeverity/Verdict)
     3) usp_SyncTrialFlagsVerdictFromRiskCategory.sql (Part 4 — two-way sync)

   GATE: staging first; production only after product sign-off.
   ============================================================================= */

SET NOCOUNT ON;

-- ---------------------------------------------------------------------------
-- Part 0 diagnostics (fixtures) — record output in the PR
-- ---------------------------------------------------------------------------
PRINT '=== Part 0: RiskCategory fixtures ===';
SELECT InterviewId, Name, Description, Severity, SeverityBreakdown, CreatedOn, ModifiedOn
FROM dbo.RiskCategory
WHERE InterviewId IN (664645, 664404, 664400, 664351)
ORDER BY InterviewId;

PRINT '=== Part 0: TrialFlags fixtures ===';
SELECT TriggeringInterviewId, OverallVerdict, Severity, FlaggedAtUtc,
       TranscriptQualityPass, ReportQualityPass, DecisionQualityPass,
       CandidateRatingPass, JoinTimePass, InterviewDurationPass
FROM dbo.TrialFlags
WHERE TriggeringInterviewId IN (664645, 664404, 664400, 664351)
ORDER BY TriggeringInterviewId;

-- Per fixture: husk = row exists with NULL Severity / empty Description; Case B = no row + persisted P0

-- ---------------------------------------------------------------------------
-- One-time husk cleanup (before sync)
-- ---------------------------------------------------------------------------
DECLARE @DeletedHusks INT;

DELETE FROM dbo.RiskCategory
WHERE Severity IS NULL
  AND ISNULL(LTRIM(RTRIM(Description)), '') = '';

SET @DeletedHusks = @@ROWCOUNT;
PRINT 'Deleted husk RiskCategory rows: ' + CAST(@DeletedHusks AS VARCHAR(20));

-- ---------------------------------------------------------------------------
-- Two-way persist sync
-- ---------------------------------------------------------------------------
DECLARE @RowsUpdated INT = 0, @RowsUpgraded INT = 0, @RowsDowngraded INT = 0;

IF OBJECT_ID(N'dbo.usp_SyncTrialFlagsVerdictFromRiskCategory', N'P') IS NOT NULL
BEGIN
    CREATE TABLE #SyncResult (
        RowsUpdated INT NULL,
        RowsUpgraded INT NULL,
        RowsDowngraded INT NULL
    );

    INSERT INTO #SyncResult (RowsUpdated, RowsUpgraded, RowsDowngraded)
    EXEC dbo.usp_SyncTrialFlagsVerdictFromRiskCategory;

    SELECT
        @RowsUpdated = ISNULL(RowsUpdated, 0),
        @RowsUpgraded = ISNULL(RowsUpgraded, 0),
        @RowsDowngraded = ISNULL(RowsDowngraded, 0)
    FROM #SyncResult;

    DROP TABLE #SyncResult;
END

PRINT 'Sync RowsUpdated: ' + CAST(@RowsUpdated AS VARCHAR(20));
PRINT 'Sync RowsUpgraded: ' + CAST(@RowsUpgraded AS VARCHAR(20));
PRINT 'Sync RowsDowngraded: ' + CAST(@RowsDowngraded AS VARCHAR(20));

-- ---------------------------------------------------------------------------
-- Post-migration fixture check (AC1–AC3)
-- ---------------------------------------------------------------------------
PRINT '=== Post-migration TrialFlags fixtures ===';
SELECT TriggeringInterviewId, OverallVerdict, Severity
FROM dbo.TrialFlags
WHERE TriggeringInterviewId IN (664645, 664404, 664400, 664351)
ORDER BY TriggeringInterviewId;

PRINT '=== Post-migration RiskCategory fixtures ===';
SELECT InterviewId, Severity, Description, SeverityBreakdown
FROM dbo.RiskCategory
WHERE InterviewId IN (664645, 664404, 664400, 664351)
ORDER BY InterviewId;

-- Remaining husks (should be 0)
SELECT COUNT(*) AS RemainingHusks
FROM dbo.RiskCategory
WHERE Severity IS NULL
  AND ISNULL(LTRIM(RTRIM(Description)), '') = '';

-- P0–P3 with empty Risk Categories must be 0 after read-proc deploy (display) + this cleanup
SELECT COUNT(*) AS StaleBadWithNoLiveRisk
FROM dbo.TrialFlags tf
LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
WHERE tf.Severity IN ('P0', 'P1', 'P2', 'P3')
  AND (rc.InterviewId IS NULL OR rc.Severity IS NULL);

SELECT
    @DeletedHusks AS DeletedHuskCount,
    @RowsDowngraded AS DowngradedTrialRowCount,
    @RowsUpgraded AS UpgradedTrialRowCount;
GO
