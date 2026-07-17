/* =============================================================================
   Trial / RiskCategory delta-4 — validation (post-deploy)
   Run AFTER Migrations/TrialRiskDelta4_CleanupHusksAndTwoWaySync.sql
   ============================================================================= */

-- AC: no P0–P3 persisted without live risk
SELECT COUNT(*) AS PersistedP0toP3WithoutLiveRisk
FROM dbo.TrialFlags tf
LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
WHERE tf.Severity IN ('P0', 'P1', 'P2', 'P3')
  AND (rc.InterviewId IS NULL OR rc.Severity IS NULL);

-- Husks gone
SELECT COUNT(*) AS RemainingHusks
FROM dbo.RiskCategory
WHERE Severity IS NULL
  AND ISNULL(LTRIM(RTRIM(Description)), '') = '';

-- Fixtures after fix
-- 664645 / 664404: expect good_trial / P5, no RiskCategory row (or empty categories)
-- 664351: expect bad_trial / P0 with AIRedo populated
SELECT tf.TriggeringInterviewId, tf.OverallVerdict, tf.Severity,
       rc.Severity AS RcSeverity, rc.Description, rc.SeverityBreakdown
FROM dbo.TrialFlags tf
LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
WHERE tf.TriggeringInterviewId IN (664645, 664404, 664400, 664351)
ORDER BY tf.TriggeringInterviewId;

-- Sort sanity: NULL severity rank must be after P5 (client uses 99; SQL check of DisplaySeverity)
-- Re-run usp_GetTrialFlagsForReview and confirm DisplaySeverity never NULL→P0 and P5 rows sort after P0–P4.
GO
