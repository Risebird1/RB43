/*
================================================================================
usp_GetTrialFlagsForReview_List — TIER 1 light grid (paginated)

MAINTAINER RULE:
  New GRID columns belong here ONLY if they are cheap keyed probes.
  Heavy JSON (MergedAIQCDetails.flags) stays in usp_GetTrialFlagDetail.
  Channel Owner = IRF.RisebirdHr → UserList (same join as usp_ShowRedoRequestLit).
  Light QC Merge uses scalar QcInterviewAIevaluation columns only — no JSON_VALUE.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagsForReview_List]
    @ReviewStatus  VARCHAR(20) = NULL,
    @Offset        INT = 0,
    @PageSize      INT = 25,
    @Search        NVARCHAR(200) = NULL,
    @Severity      VARCHAR(4) = NULL,
    @ChannelOwner  NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    IF @Offset IS NULL OR @Offset < 0 SET @Offset = 0;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 25;
    IF @PageSize > 200 SET @PageSize = 200;

    DECLARE @SearchTrim NVARCHAR(200) = NULLIF(LTRIM(RTRIM(@Search)), N'');
    DECLARE @SeverityTrim VARCHAR(4) = NULLIF(UPPER(LTRIM(RTRIM(@Severity))), '');
    DECLARE @OwnerTrim NVARCHAR(200) = NULLIF(LTRIM(RTRIM(@ChannelOwner)), N'');

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT
            CAST(0 AS INT) AS TrialFlagId,
            CAST(0 AS INT) AS InterviewerId,
            CAST(0 AS INT) AS IrfId,
            CAST(0 AS INT) AS ChannelId,
            CAST(0 AS INT) AS TriggeringInterviewId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST('' AS VARCHAR(30)) AS OverallVerdict,
            CAST('' AS VARCHAR(30)) AS DisplayVerdict,
            CAST(NULL AS VARCHAR(4)) AS Severity,
            CAST('' AS VARCHAR(20)) AS ReviewStatus,
            CAST(NULL AS DATETIME) AS FlaggedAtUtc,
            CAST(NULL AS DATETIME) AS ReviewedAtUtc,
            CAST(NULL AS VARCHAR(150)) AS ReviewedBy,
            CAST(NULL AS NVARCHAR(500)) AS ReviewerComment,
            CAST('' AS NVARCHAR(200)) AS InterviewerName,
            CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId,
            CAST(NULL AS INT) AS InterviewerUserId,
            CAST(NULL AS VARCHAR(20)) AS InterviewerMobileNumber,
            CAST(NULL AS NVARCHAR(200)) AS InterviewerAltMobileNumber,
            CAST(0 AS BIT) AS IsSuspicious,
            CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(NULL AS VARCHAR(200)) AS ChannelOwnerEmail,
            CAST(NULL AS NVARCHAR(300)) AS ChannelOwner,
            CAST(NULL AS VARCHAR(MAX)) AS RiskCategoryLabels,
            CAST(NULL AS VARCHAR(500)) AS RiskCategoryReasons,
            CAST(NULL AS VARCHAR(20)) AS QcMergeVerdict,
            CAST(NULL AS INT) AS QcMergeTotalPenalty,
            CAST(NULL AS INT) AS QcMergeQcScore,
            CAST(NULL AS INT) AS QcMergeScore,
            CAST(0 AS INT) AS TotalRows
        WHERE 1 = 0;
        RETURN;
    END;

    DECLARE @HasAiMergePenalty BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'AIMergeTotalPenalty') IS NOT NULL THEN 1 ELSE 0 END;

    ;WITH Base AS (
        SELECT
            tf.Id AS TrialFlagId,
            tf.InterviewerId,
            tf.IrfId,
            tf.ChannelId,
            tf.TriggeringInterviewId,
            tf.TriggeringInterviewDate,
            tf.OverallVerdict,
            CASE
                WHEN rc.Severity IS NOT NULL THEN 'bad_trial'
                WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1
                 AND ISNULL(tf.ReportQualityPass, 0) = 1
                 AND ISNULL(tf.DecisionQualityPass, 0) = 1
                 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                 AND ISNULL(tf.JoinTimePass, 0) = 1
                 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                THEN 'good_trial'
                ELSE 'bad_trial'
            END AS DisplayVerdict,
            CASE
                WHEN rc.Severity IS NOT NULL THEN rc.Severity
                WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1
                 AND ISNULL(tf.ReportQualityPass, 0) = 1
                 AND ISNULL(tf.DecisionQualityPass, 0) = 1
                 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                 AND ISNULL(tf.JoinTimePass, 0) = 1
                 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                THEN 'P5'
                ELSE 'P4'
            END AS Severity,
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
            ru.UserId AS InterviewerUserId,
            CASE
                WHEN ru.MobileNumber IS NULL THEN NULL
                WHEN LTRIM(RTRIM(CAST(ru.MobileNumber AS VARCHAR(30)))) IN ('', '0') THEN NULL
                ELSE LTRIM(RTRIM(CAST(ru.MobileNumber AS VARCHAR(30))))
            END AS InterviewerMobileNumber,
            CASE
                WHEN NULLIF(LTRIM(RTRIM(ip.AltMobileNumber)), '') IS NULL THEN NULL
                WHEN LTRIM(RTRIM(ip.AltMobileNumber)) IN ('0', '+0') THEN NULL
                ELSE LTRIM(RTRIM(ip.AltMobileNumber))
            END AS InterviewerAltMobileNumber,
            CAST(CASE WHEN ISNULL(si.id, 0) > 0 THEN 1 ELSE 0 END AS BIT) AS IsSuspicious,
            COALESCE(
                NULLIF(LTRIM(RTRIM(irf.IRFId)), ''),
                NULLIF(LTRIM(RTRIM(irfViaInterview.IRFId)), ''),
                NULLIF(LTRIM(RTRIM(im.IRFId)), ''),
                CASE WHEN tf.IrfId > 0 THEN 'IRF #' + CAST(tf.IrfId AS VARCHAR(12)) ELSE '' END
            ) AS IrfName,
            NULLIF(LTRIM(RTRIM(ulco.EmailId)), '') AS ChannelOwnerEmail,
            CASE
                WHEN NULLIF(LTRIM(RTRIM(ulco.EmailId)), '') IS NULL THEN NULL
                WHEN ulco.LastName IS NULL AND NULLIF(LTRIM(RTRIM(ulco.FirstName)), '') IS NOT NULL
                    THEN LTRIM(RTRIM(ulco.FirstName)) + N' (' + LTRIM(RTRIM(ulco.EmailId)) + N')'
                WHEN NULLIF(LTRIM(RTRIM(ulco.FirstName)), '') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ulco.LastName)), '') IS NOT NULL
                    THEN LTRIM(RTRIM(CONCAT(ISNULL(ulco.FirstName, ''), N' ', ISNULL(ulco.LastName, ''))))
                         + N' (' + LTRIM(RTRIM(ulco.EmailId)) + N')'
                ELSE LTRIM(RTRIM(ulco.EmailId))
            END AS ChannelOwner,
            rc.Name AS RiskCategoryLabels,
            rc.Description AS RiskCategoryReasons,
            -- Light QC (no JSON_VALUE): scalar columns only
            CASE
                WHEN q.InterviewId IS NULL THEN NULL
                WHEN ISNULL(q.AIMergeRedoInterview, 0) = 1 THEN 'redo'
                WHEN UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) IN ('YES', 'Y', 'TRUE') THEN 'suspicious'
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(q.qcmergeverdict, ''))), '') IS NOT NULL THEN LTRIM(RTRIM(q.qcmergeverdict))
                ELSE 'ok'
            END AS QcMergeVerdict,
            CASE WHEN @HasAiMergePenalty = 1 THEN q.AIMergeTotalPenalty ELSE NULL END AS QcMergeTotalPenalty,
            q.qcmergescore AS QcMergeQcScore,
            CASE WHEN @HasAiMergePenalty = 1 THEN q.AIMergeTotalPenalty ELSE NULL END AS QcMergeScore,
            CASE
                WHEN rc.Severity IS NOT NULL THEN
                    CASE UPPER(LTRIM(RTRIM(rc.Severity)))
                        WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
                        WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5
                        ELSE 98
                    END
                WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1
                 AND ISNULL(tf.ReportQualityPass, 0) = 1
                 AND ISNULL(tf.DecisionQualityPass, 0) = 1
                 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                 AND ISNULL(tf.JoinTimePass, 0) = 1
                 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                THEN 5
                ELSE 4
            END AS SeveritySort
        FROM dbo.TrialFlags tf
        LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
        LEFT JOIN dbo.TransInterviewer ti ON ti.id = tf.ChannelId
        LEFT JOIN dbo.IRF irf ON irf.id = tf.IrfId
        LEFT JOIN dbo.UserList ulco ON ulco.id = irf.RisebirdHr
        LEFT JOIN dbo.InterviewMeetings im ON im.id = tf.TriggeringInterviewId
        LEFT JOIN dbo.IRF irfViaInterview ON irfViaInterview.IRFId = im.IRFId
        LEFT JOIN dbo.Interviewer iv ON iv.id = COALESCE(ti.InterviewerId, tf.InterviewerId)
        LEFT JOIN dbo.InterviewerProfile ipViaTi ON ipViaTi.id = COALESCE(ti.InterviewerId, tf.InterviewerId)
        LEFT JOIN dbo.UserList ulViaInterviewer ON ulViaInterviewer.id = iv.UserId
        LEFT JOIN dbo.InterviewerProfile ipViaInterviewer ON ipViaInterviewer.UserId = iv.UserId
        OUTER APPLY (
            SELECT TOP 1
                qq.InterviewId,
                qq.qcmergeverdict,
                qq.qcmergescore,
                qq.AIMergeRedoInterview,
                qq.AIMergeInterviewerSuspicious,
                CASE WHEN @HasAiMergePenalty = 1 THEN qq.AIMergeTotalPenalty END AS AIMergeTotalPenalty
            FROM dbo.QcInterviewAIevaluation qq
            WHERE qq.InterviewId = tf.TriggeringInterviewId
            ORDER BY qq.id DESC
        ) q
        OUTER APPLY (
            SELECT TOP 1 x.UserId, x.EmailId, x.MobileNumber
            FROM (
                SELECT ul.id AS UserId, ul.EmailId, ul.MobileNumber, 1 AS Priority
                FROM dbo.UserList ul
                WHERE im.interviewer IS NOT NULL
                  AND LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(im.interviewer))
                UNION ALL
                SELECT ul.id, ul.EmailId, ul.MobileNumber, 2
                FROM dbo.UserList ul
                WHERE im.interviewer IS NOT NULL AND ul.EmailId = im.interviewer
                UNION ALL
                SELECT ip.UserId, ul.EmailId, ul.MobileNumber, 3
                FROM dbo.InterviewerProfile ip
                INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
                WHERE im.interviewer IS NOT NULL
                  AND LTRIM(RTRIM(ip.NickName)) = LTRIM(RTRIM(im.interviewer))
                UNION ALL
                SELECT ul.id, ul.EmailId, ul.MobileNumber, 4
                FROM dbo.UserList ul
                WHERE ti.UserId IS NOT NULL AND ul.id = ti.UserId
                UNION ALL
                SELECT ul.id, ul.EmailId, ul.MobileNumber, 5
                FROM dbo.UserList ul
                WHERE iv.UserId IS NOT NULL AND ul.id = iv.UserId
                UNION ALL
                SELECT ul.id, ul.EmailId, ul.MobileNumber, 6
                FROM dbo.UserList ul
                WHERE ipViaTi.UserId IS NOT NULL AND ul.id = ipViaTi.UserId
            ) x
            WHERE NULLIF(LTRIM(RTRIM(x.EmailId)), '') IS NOT NULL OR x.UserId IS NOT NULL
            ORDER BY x.Priority
        ) ru
        LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ru.UserId
        LEFT JOIN dbo.SuspiciousInterviewer si ON si.UserId = ru.UserId AND ISNULL(si.SuspiciousStatus, 0) = 1
        WHERE (@ReviewStatus IS NULL OR tf.ReviewStatus = @ReviewStatus)
          AND (
                @SearchTrim IS NULL
             OR COALESCE(irf.IRFId, irfViaInterview.IRFId, im.IRFId, '') LIKE N'%' + @SearchTrim + N'%'
             OR COALESCE(ru.EmailId, ulViaInterviewer.EmailId, im.interviewer, '') LIKE N'%' + @SearchTrim + N'%'
             OR COALESCE(ip.NickName, ipViaTi.NickName, ipViaInterviewer.NickName, '') LIKE N'%' + @SearchTrim + N'%'
             OR COALESCE(ulco.EmailId, '') LIKE N'%' + @SearchTrim + N'%'
             OR COALESCE(ulco.FirstName, '') LIKE N'%' + @SearchTrim + N'%'
             OR COALESCE(ulco.LastName, '') LIKE N'%' + @SearchTrim + N'%'
             OR CAST(tf.TriggeringInterviewId AS VARCHAR(20)) LIKE N'%' + @SearchTrim + N'%'
          )
          AND (
                @OwnerTrim IS NULL
             OR LTRIM(RTRIM(ulco.EmailId)) = @OwnerTrim
          )
    )
    SELECT
        b.TrialFlagId, b.InterviewerId, b.IrfId, b.ChannelId, b.TriggeringInterviewId, b.TriggeringInterviewDate,
        b.OverallVerdict, b.DisplayVerdict, b.Severity, b.ReviewStatus, b.FlaggedAtUtc, b.ReviewedAtUtc,
        b.ReviewedBy, b.ReviewerComment, b.InterviewerName, b.InterviewerEmail, b.InterviewerCodedId,
        b.InterviewerUserId, b.InterviewerMobileNumber, b.InterviewerAltMobileNumber, b.IsSuspicious,
        b.IrfName, b.ChannelOwnerEmail, b.ChannelOwner, b.RiskCategoryLabels, b.RiskCategoryReasons,
        b.QcMergeVerdict, b.QcMergeTotalPenalty, b.QcMergeQcScore, b.QcMergeScore,
        COUNT(1) OVER() AS TotalRows
    FROM Base b
    WHERE (@SeverityTrim IS NULL OR UPPER(LTRIM(RTRIM(b.Severity))) = @SeverityTrim)
    ORDER BY b.SeveritySort ASC, b.FlaggedAtUtc DESC, b.TrialFlagId DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
