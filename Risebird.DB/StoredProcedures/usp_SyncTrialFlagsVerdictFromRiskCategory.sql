CREATE OR ALTER PROCEDURE [dbo].[usp_SyncTrialFlagsVerdictFromRiskCategory]
AS
/*
  Two-way persist sync between TrialFlags and live RiskCategory for TriggeringInterviewId.
  Upgrade:  good_trial + live risk (rc.Severity NOT NULL) -> bad_trial, Severity = rc.Severity
  Downgrade: live risk absent or NULL-severity husk ->
             Severity = P4 if any factor bit failed else P5;
             OverallVerdict = bad_trial if P4 else good_trial
  Returns: RowsUpgraded, RowsDowngraded (and RowsUpdated = sum for callers that read one column).
*/
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL OR OBJECT_ID(N'dbo.RiskCategory', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS INT) AS RowsUpdated, CAST(0 AS INT) AS RowsUpgraded, CAST(0 AS INT) AS RowsDowngraded;
        RETURN;
    END

    DECLARE @Upgraded INT = 0;
    DECLARE @Downgraded INT = 0;

    -- Upgrade (existing): good_trial + live risk -> bad_trial + rc.Severity
    UPDATE tf
    SET
        tf.OverallVerdict = 'bad_trial',
        tf.Severity = rc.Severity
    FROM dbo.TrialFlags tf
    INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
    WHERE tf.OverallVerdict = 'good_trial'
      AND rc.Severity IS NOT NULL;

    SET @Upgraded = @@ROWCOUNT;

    -- Downgrade (NEW): no live risk (no row, or husk with NULL Severity)
    ;WITH Targets AS (
        SELECT
            tf.Id,
            CASE
                WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1
                 AND ISNULL(tf.ReportQualityPass, 0) = 1
                 AND ISNULL(tf.DecisionQualityPass, 0) = 1
                 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                 AND ISNULL(tf.JoinTimePass, 0) = 1
                 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                THEN 'P5'
                ELSE 'P4'
            END AS NewSeverity
        FROM dbo.TrialFlags tf
        LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
        WHERE rc.InterviewId IS NULL OR rc.Severity IS NULL
    )
    UPDATE tf
    SET
        tf.Severity = t.NewSeverity,
        tf.OverallVerdict = CASE WHEN t.NewSeverity = 'P4' THEN 'bad_trial' ELSE 'good_trial' END
    FROM dbo.TrialFlags tf
    INNER JOIN Targets t ON t.Id = tf.Id
    WHERE ISNULL(tf.Severity, '') <> t.NewSeverity
       OR tf.OverallVerdict <> CASE WHEN t.NewSeverity = 'P4' THEN 'bad_trial' ELSE 'good_trial' END;

    SET @Downgraded = @@ROWCOUNT;

    SELECT
        (@Upgraded + @Downgraded) AS RowsUpdated,
        @Upgraded AS RowsUpgraded,
        @Downgraded AS RowsDowngraded;
END
GO
