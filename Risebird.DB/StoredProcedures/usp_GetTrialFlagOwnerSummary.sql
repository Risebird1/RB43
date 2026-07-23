/*
================================================================================
usp_GetTrialFlagOwnerSummary — cheap per-owner P0 + total counts for active tab.
No JSON / PaymentSLA. Used by the Channel Owner filter summary strip.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagOwnerSummary]
    @ReviewStatus VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT
            CAST(NULL AS VARCHAR(200)) AS ChannelOwnerEmail,
            CAST(NULL AS NVARCHAR(300)) AS ChannelOwner,
            CAST(0 AS INT) AS P0Count,
            CAST(0 AS INT) AS TotalCount
        WHERE 1 = 0;
        RETURN;
    END;

    ;WITH Rows AS (
        SELECT
            NULLIF(LTRIM(RTRIM(ulco.EmailId)), '') AS ChannelOwnerEmail,
            CASE
                WHEN NULLIF(LTRIM(RTRIM(ulco.EmailId)), '') IS NULL THEN N'(unassigned)'
                WHEN ulco.LastName IS NULL AND NULLIF(LTRIM(RTRIM(ulco.FirstName)), '') IS NOT NULL
                    THEN LTRIM(RTRIM(ulco.FirstName)) + N' (' + LTRIM(RTRIM(ulco.EmailId)) + N')'
                WHEN NULLIF(LTRIM(RTRIM(ulco.FirstName)), '') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ulco.LastName)), '') IS NOT NULL
                    THEN LTRIM(RTRIM(CONCAT(ISNULL(ulco.FirstName, ''), N' ', ISNULL(ulco.LastName, ''))))
                         + N' (' + LTRIM(RTRIM(ulco.EmailId)) + N')'
                ELSE LTRIM(RTRIM(ulco.EmailId))
            END AS ChannelOwner,
            CASE
                WHEN rc.Severity IS NOT NULL THEN UPPER(LTRIM(RTRIM(rc.Severity)))
                WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1
                 AND ISNULL(tf.ReportQualityPass, 0) = 1
                 AND ISNULL(tf.DecisionQualityPass, 0) = 1
                 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                 AND ISNULL(tf.JoinTimePass, 0) = 1
                 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                THEN 'P5'
                ELSE 'P4'
            END AS Severity
        FROM dbo.TrialFlags tf
        LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
        LEFT JOIN dbo.IRF irf ON irf.id = tf.IrfId
        LEFT JOIN dbo.UserList ulco ON ulco.id = irf.RisebirdHr
        WHERE (@ReviewStatus IS NULL OR tf.ReviewStatus = @ReviewStatus)
    )
    SELECT
        ChannelOwnerEmail,
        ChannelOwner,
        SUM(CASE WHEN Severity = 'P0' THEN 1 ELSE 0 END) AS P0Count,
        COUNT(1) AS TotalCount
    FROM Rows
    GROUP BY ChannelOwnerEmail, ChannelOwner
    ORDER BY P0Count DESC, TotalCount DESC, ChannelOwner;
END
GO
