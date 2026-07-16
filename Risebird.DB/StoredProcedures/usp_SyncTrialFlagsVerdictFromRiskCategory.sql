CREATE OR ALTER PROCEDURE [dbo].[usp_SyncTrialFlagsVerdictFromRiskCategory]
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL OR OBJECT_ID(N'dbo.RiskCategory', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS INT) AS RowsUpdated;
        RETURN;
    END

    -- Persist bad_trial + Severity whenever RiskCategory exists for the triggering interview
    -- but TrialFlags still says good_trial (stale after late QC / video-off regrade).
    UPDATE tf
    SET
        tf.OverallVerdict = 'bad_trial',
        tf.Severity = rc.Severity
    FROM dbo.TrialFlags tf
    INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
    WHERE tf.OverallVerdict = 'good_trial';

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO
