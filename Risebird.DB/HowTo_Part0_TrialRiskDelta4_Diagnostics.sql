/* =============================================================================
   Trial / RiskCategory delta-4 — Part 0 diagnostics ONLY (read-only)
   Record each fixture as: husk row (row exists, NULL fields) OR Case B
   (no RiskCategory row, TrialFlags.Severity = P0).
   ============================================================================= */

SELECT InterviewId, Name, Description, Severity, SeverityBreakdown, CreatedOn, ModifiedOn
FROM dbo.RiskCategory
WHERE InterviewId IN (664645, 664404, 664400, 664351)
ORDER BY InterviewId;

SELECT TriggeringInterviewId, OverallVerdict, Severity, FlaggedAtUtc,
       TranscriptQualityPass, ReportQualityPass, DecisionQualityPass,
       CandidateRatingPass, JoinTimePass, InterviewDurationPass
FROM dbo.TrialFlags
WHERE TriggeringInterviewId IN (664645, 664404, 664400, 664351)
ORDER BY TriggeringInterviewId;

-- Husk candidates globally
SELECT COUNT(*) AS HuskCandidateCount
FROM dbo.RiskCategory
WHERE Severity IS NULL
  AND ISNULL(LTRIM(RTRIM(Description)), '') = '';

-- Case B candidates: trial P0 with no live risk severity
SELECT tf.TriggeringInterviewId, tf.OverallVerdict, tf.Severity AS PersistedSeverity,
       rc.InterviewId AS RcInterviewId, rc.Severity AS RcSeverity, rc.Description AS RcDescription
FROM dbo.TrialFlags tf
LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
WHERE tf.TriggeringInterviewId IN (664645, 664404, 664400, 664351);
GO
