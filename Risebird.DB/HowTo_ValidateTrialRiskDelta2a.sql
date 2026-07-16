/* =============================================================================
   Trial / RiskCategory delta-2a — post-deploy validation queries
   Run AFTER:
     1) usp_AddUpdateRiskCategory.sql (video-off fix)
     2) RiskCategory backfill (Refresh live metrics on Trial page, and/or
        IRF Audit Refresh risk over last 90 days)
     3) usp_SyncTrialFlagsVerdictFromRiskCategory.sql
   ============================================================================= */

-- Severity distribution (record before + after backfill in the PR)
SELECT Severity, COUNT(*) AS Cnt
FROM dbo.RiskCategory
GROUP BY Severity
ORDER BY Severity;

-- InterviewerVideoOff still present in SeverityBreakdown
SELECT COUNT(*) AS VideoOffStillPresent
FROM dbo.RiskCategory
WHERE SeverityBreakdown LIKE '%InterviewerVideoOff%';

-- Stale good_trial with a RiskCategory row (should be 0 after sync)
SELECT COUNT(*) AS StaleGoodTrialWithRisk
FROM dbo.TrialFlags tf
INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
WHERE tf.OverallVerdict = 'good_trial';
GO
