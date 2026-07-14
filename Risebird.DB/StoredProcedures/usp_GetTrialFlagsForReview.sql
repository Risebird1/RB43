CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagsForReview]
    @ReviewStatus VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT
            CAST(0 AS INT) AS TrialFlagId,
            CAST(0 AS INT) AS InterviewerId,
            CAST(0 AS INT) AS IrfId,
            CAST(0 AS INT) AS ChannelId,
            CAST(0 AS INT) AS TriggeringInterviewId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST(0 AS INT) AS TranscriptQualityPass,
            CAST(0 AS INT) AS ReportQualityPass,
            CAST(0 AS INT) AS DecisionQualityPass,
            CAST(0 AS INT) AS CandidateRatingPass,
            CAST(0 AS INT) AS JoinTimePass,
            CAST(0 AS INT) AS InterviewDurationPass,
            CAST(NULL AS DECIMAL(5,2)) AS TranscriptQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS CandidateRating,
            CAST(NULL AS DECIMAL(8,2)) AS JoinTimeMinutes,
            CAST(NULL AS DECIMAL(8,2)) AS InterviewDurationMinutes,
            CAST('' AS VARCHAR(30)) AS OverallVerdict,
            CAST(NULL AS VARCHAR(4)) AS Severity,
            CAST('' AS VARCHAR(20)) AS ReviewStatus,
            CAST(NULL AS DATETIME) AS FlaggedAtUtc,
            CAST(NULL AS DATETIME) AS ReviewedAtUtc,
            CAST(NULL AS VARCHAR(150)) AS ReviewedBy,
            CAST(NULL AS NVARCHAR(500)) AS ReviewerComment,
            CAST('' AS NVARCHAR(200)) AS InterviewerName,
            CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId,
            CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(NULL AS VARCHAR(MAX)) AS RiskCategoryLabels,
            CAST(NULL AS VARCHAR(500)) AS RiskCategoryReasons,
            CAST(NULL AS VARCHAR(500)) AS SeverityBreakdown
        WHERE 1 = 0;
        RETURN;
    END

    SELECT
        tf.Id AS TrialFlagId,
        tf.InterviewerId,
        tf.IrfId,
        tf.ChannelId,
        tf.TriggeringInterviewId,
        tf.TriggeringInterviewDate,
        CAST(tf.TranscriptQualityPass AS INT) AS TranscriptQualityPass,
        CAST(tf.ReportQualityPass AS INT) AS ReportQualityPass,
        CAST(tf.DecisionQualityPass AS INT) AS DecisionQualityPass,
        CAST(tf.CandidateRatingPass AS INT) AS CandidateRatingPass,
        CAST(tf.JoinTimePass AS INT) AS JoinTimePass,
        CAST(tf.InterviewDurationPass AS INT) AS InterviewDurationPass,
        tf.TranscriptQualityScore,
        tf.CandidateRating,
        tf.JoinTimeMinutes,
        tf.InterviewDurationMinutes,
        tf.OverallVerdict,
        COALESCE(rc.Severity, tf.Severity) AS Severity,
        tf.ReviewStatus,
        tf.FlaggedAtUtc,
        tf.ReviewedAtUtc,
        tf.ReviewedBy,
        tf.ReviewerComment,
        COALESCE(
            NULLIF(LTRIM(RTRIM(ru.EmailId)), ''),
            NULLIF(LTRIM(RTRIM(ulViaInterviewer.EmailId)), ''),
            NULLIF(LTRIM(RTRIM(im.interviewer)), ''),
            'Interviewer #' + CAST(tf.InterviewerId AS VARCHAR(12))
        ) AS InterviewerName,
        COALESCE(
            NULLIF(LTRIM(RTRIM(ru.EmailId)), ''),
            NULLIF(LTRIM(RTRIM(ulViaInterviewer.EmailId)), ''),
            NULLIF(LTRIM(RTRIM(im.interviewer)), ''),
            ''
        ) AS InterviewerEmail,
        COALESCE(
            NULLIF(LTRIM(RTRIM(ip.NickName)), ''),
            NULLIF(LTRIM(RTRIM(ipViaTi.NickName)), ''),
            NULLIF(LTRIM(RTRIM(ipViaInterviewer.NickName)), ''),
            ''
        ) AS InterviewerCodedId,
        COALESCE(
            NULLIF(LTRIM(RTRIM(irf.IRFId)), ''),
            NULLIF(LTRIM(RTRIM(irfViaInterview.IRFId)), ''),
            NULLIF(LTRIM(RTRIM(im.IRFId)), ''),
            CASE WHEN tf.IrfId > 0 THEN 'IRF #' + CAST(tf.IrfId AS VARCHAR(12)) ELSE '' END
        ) AS IrfName,
        rc.Name AS RiskCategoryLabels,
        rc.Description AS RiskCategoryReasons,
        rc.SeverityBreakdown
    FROM dbo.TrialFlags tf
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
    LEFT JOIN dbo.TransInterviewer ti ON ti.id = tf.ChannelId
    LEFT JOIN dbo.IRF irf ON irf.id = tf.IrfId
    LEFT JOIN dbo.InterviewMeetings im ON im.id = tf.TriggeringInterviewId
    LEFT JOIN dbo.IRF irfViaInterview ON irfViaInterview.IRFId = im.IRFId
    LEFT JOIN dbo.Interviewer iv ON iv.id = COALESCE(ti.InterviewerId, tf.InterviewerId)
    LEFT JOIN dbo.InterviewerProfile ipViaTi ON ipViaTi.id = COALESCE(ti.InterviewerId, tf.InterviewerId)
    LEFT JOIN dbo.UserList ulViaInterviewer ON ulViaInterviewer.id = iv.UserId
    LEFT JOIN dbo.InterviewerProfile ipViaInterviewer ON ipViaInterviewer.UserId = iv.UserId
    OUTER APPLY (
        SELECT TOP 1 x.UserId, x.EmailId
        FROM (
            SELECT ul.id AS UserId, ul.EmailId, 1 AS Priority
            FROM dbo.UserList ul
            WHERE im.interviewer IS NOT NULL
              AND LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(im.interviewer))
            UNION ALL
            SELECT ul.id, ul.EmailId, 2
            FROM dbo.UserList ul
            WHERE im.interviewer IS NOT NULL AND ul.EmailId = im.interviewer
            UNION ALL
            SELECT ip.UserId, ul.EmailId, 3
            FROM dbo.InterviewerProfile ip
            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
            WHERE im.interviewer IS NOT NULL
              AND LTRIM(RTRIM(ip.NickName)) = LTRIM(RTRIM(im.interviewer))
            UNION ALL
            SELECT ul.id AS UserId, ul.EmailId, 4 AS Priority
            FROM dbo.UserList ul
            WHERE ti.UserId IS NOT NULL AND ul.id = ti.UserId
            UNION ALL
            SELECT ul.id, ul.EmailId, 5
            FROM dbo.UserList ul
            WHERE iv.UserId IS NOT NULL AND ul.id = iv.UserId
            UNION ALL
            SELECT ul.id, ul.EmailId, 6
            FROM dbo.UserList ul
            WHERE ipViaTi.UserId IS NOT NULL AND ul.id = ipViaTi.UserId
        ) x
        WHERE NULLIF(LTRIM(RTRIM(x.EmailId)), '') IS NOT NULL OR x.UserId IS NOT NULL
        ORDER BY x.Priority
    ) ru
    LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ru.UserId
    WHERE (@ReviewStatus IS NULL OR tf.ReviewStatus = @ReviewStatus)
    ORDER BY tf.FlaggedAtUtc DESC, tf.Id DESC;
END
GO