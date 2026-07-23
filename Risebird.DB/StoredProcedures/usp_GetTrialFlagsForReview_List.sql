/*
================================================================================
usp_GetTrialFlagsForReview_List — TIER 1 light grid (paginated)

PERFORMANCE (fixes "wait operation timed out" on Pending):
  Stage 1 — filter/sort/page on TrialFlags + RiskCategory only (default pending load).
             Search/owner joins run only when those filters are set.
  Stage 2 — enrich ONLY the paged rows (≤ @PageSize) with QC + interviewer APPLY.

  Never run UserList multi-branch APPLY or QC APPLY across the full queue.
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
    DECLARE @StatusTrim VARCHAR(20) = NULLIF(LOWER(LTRIM(RTRIM(@ReviewStatus))), '');

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT TOP (0)
            CAST(0 AS INT) AS TrialFlagId, CAST(0 AS INT) AS InterviewerId, CAST(0 AS INT) AS IrfId,
            CAST(0 AS INT) AS ChannelId, CAST(0 AS INT) AS TriggeringInterviewId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST('' AS VARCHAR(30)) AS OverallVerdict, CAST('' AS VARCHAR(30)) AS DisplayVerdict,
            CAST(NULL AS VARCHAR(4)) AS Severity, CAST('' AS VARCHAR(20)) AS ReviewStatus,
            CAST(NULL AS DATETIME) AS FlaggedAtUtc, CAST(NULL AS DATETIME) AS ReviewedAtUtc,
            CAST(NULL AS VARCHAR(150)) AS ReviewedBy, CAST(NULL AS NVARCHAR(500)) AS ReviewerComment,
            CAST('' AS NVARCHAR(200)) AS InterviewerName, CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId, CAST(NULL AS INT) AS InterviewerUserId,
            CAST(NULL AS VARCHAR(20)) AS InterviewerMobileNumber,
            CAST(NULL AS NVARCHAR(200)) AS InterviewerAltMobileNumber, CAST(0 AS BIT) AS IsSuspicious,
            CAST('' AS NVARCHAR(200)) AS IrfName, CAST(NULL AS VARCHAR(200)) AS ChannelOwnerEmail,
            CAST(NULL AS NVARCHAR(300)) AS ChannelOwner, CAST(NULL AS VARCHAR(MAX)) AS RiskCategoryLabels,
            CAST(NULL AS VARCHAR(500)) AS RiskCategoryReasons, CAST(NULL AS VARCHAR(20)) AS QcMergeVerdict,
            CAST(NULL AS INT) AS QcMergeTotalPenalty, CAST(NULL AS INT) AS QcMergeQcScore,
            CAST(NULL AS INT) AS QcMergeScore, CAST(0 AS INT) AS TotalRows;
        RETURN;
    END;

    DECLARE @HasAiMergePenalty BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'AIMergeTotalPenalty') IS NOT NULL THEN 1 ELSE 0 END;

    IF OBJECT_ID('tempdb..#TfrPage') IS NOT NULL DROP TABLE #TfrPage;
    CREATE TABLE #TfrPage (
        TrialFlagId INT NOT NULL PRIMARY KEY,
        InterviewerId INT NOT NULL,
        IrfId INT NOT NULL,
        ChannelId INT NOT NULL,
        TriggeringInterviewId INT NOT NULL,
        TriggeringInterviewDate DATETIME NOT NULL,
        OverallVerdict VARCHAR(30) NULL,
        ReviewStatus VARCHAR(20) NULL,
        FlaggedAtUtc DATETIME NULL,
        ReviewedAtUtc DATETIME NULL,
        ReviewedBy VARCHAR(150) NULL,
        ReviewerComment NVARCHAR(500) NULL,
        TranscriptQualityPass BIT NULL,
        ReportQualityPass BIT NULL,
        DecisionQualityPass BIT NULL,
        CandidateRatingPass BIT NULL,
        JoinTimePass BIT NULL,
        InterviewDurationPass BIT NULL,
        Severity VARCHAR(4) NULL,
        SeveritySort INT NOT NULL,
        TotalRows INT NOT NULL
    );

    /* -------- Stage 1: page ids -------- */
    IF @SearchTrim IS NULL AND @OwnerTrim IS NULL
    BEGIN
        ;WITH Ranked AS (
            SELECT
                tf.Id AS TrialFlagId,
                tf.InterviewerId, tf.IrfId, tf.ChannelId,
                tf.TriggeringInterviewId, tf.TriggeringInterviewDate,
                tf.OverallVerdict, tf.ReviewStatus, tf.FlaggedAtUtc, tf.ReviewedAtUtc,
                tf.ReviewedBy, tf.ReviewerComment,
                tf.TranscriptQualityPass, tf.ReportQualityPass, tf.DecisionQualityPass,
                tf.CandidateRatingPass, tf.JoinTimePass, tf.InterviewDurationPass,
                CASE
                    WHEN rc.Severity IS NOT NULL THEN rc.Severity
                    WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1 AND ISNULL(tf.ReportQualityPass, 0) = 1
                     AND ISNULL(tf.DecisionQualityPass, 0) = 1 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                     AND ISNULL(tf.JoinTimePass, 0) = 1 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                    THEN 'P5' ELSE 'P4'
                END AS Severity,
                CASE
                    WHEN rc.Severity IS NOT NULL THEN
                        CASE UPPER(LTRIM(RTRIM(rc.Severity)))
                            WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
                            WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 98 END
                    WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1 AND ISNULL(tf.ReportQualityPass, 0) = 1
                     AND ISNULL(tf.DecisionQualityPass, 0) = 1 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                     AND ISNULL(tf.JoinTimePass, 0) = 1 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                    THEN 5 ELSE 4
                END AS SeveritySort
            FROM dbo.TrialFlags tf
            LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
            WHERE (@StatusTrim IS NULL OR LOWER(LTRIM(RTRIM(ISNULL(tf.ReviewStatus, '')))) = @StatusTrim)
        ),
        Filtered AS (
            SELECT *, COUNT(1) OVER() AS TotalRows
            FROM Ranked
            WHERE (@SeverityTrim IS NULL OR UPPER(LTRIM(RTRIM(Severity))) = @SeverityTrim)
        )
        INSERT INTO #TfrPage (
            TrialFlagId, InterviewerId, IrfId, ChannelId, TriggeringInterviewId, TriggeringInterviewDate,
            OverallVerdict, ReviewStatus, FlaggedAtUtc, ReviewedAtUtc, ReviewedBy, ReviewerComment,
            TranscriptQualityPass, ReportQualityPass, DecisionQualityPass,
            CandidateRatingPass, JoinTimePass, InterviewDurationPass,
            Severity, SeveritySort, TotalRows)
        SELECT
            TrialFlagId, InterviewerId, IrfId, ChannelId, TriggeringInterviewId, TriggeringInterviewDate,
            OverallVerdict, ReviewStatus, FlaggedAtUtc, ReviewedAtUtc, ReviewedBy, ReviewerComment,
            TranscriptQualityPass, ReportQualityPass, DecisionQualityPass,
            CandidateRatingPass, JoinTimePass, InterviewDurationPass,
            Severity, SeveritySort, TotalRows
        FROM Filtered
        ORDER BY SeveritySort ASC, FlaggedAtUtc DESC, TrialFlagId DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
    END
    ELSE
    BEGIN
        ;WITH Ranked AS (
            SELECT
                tf.Id AS TrialFlagId,
                tf.InterviewerId, tf.IrfId, tf.ChannelId,
                tf.TriggeringInterviewId, tf.TriggeringInterviewDate,
                tf.OverallVerdict, tf.ReviewStatus, tf.FlaggedAtUtc, tf.ReviewedAtUtc,
                tf.ReviewedBy, tf.ReviewerComment,
                tf.TranscriptQualityPass, tf.ReportQualityPass, tf.DecisionQualityPass,
                tf.CandidateRatingPass, tf.JoinTimePass, tf.InterviewDurationPass,
                CASE
                    WHEN rc.Severity IS NOT NULL THEN rc.Severity
                    WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1 AND ISNULL(tf.ReportQualityPass, 0) = 1
                     AND ISNULL(tf.DecisionQualityPass, 0) = 1 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                     AND ISNULL(tf.JoinTimePass, 0) = 1 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                    THEN 'P5' ELSE 'P4'
                END AS Severity,
                CASE
                    WHEN rc.Severity IS NOT NULL THEN
                        CASE UPPER(LTRIM(RTRIM(rc.Severity)))
                            WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
                            WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 98 END
                    WHEN ISNULL(tf.TranscriptQualityPass, 0) = 1 AND ISNULL(tf.ReportQualityPass, 0) = 1
                     AND ISNULL(tf.DecisionQualityPass, 0) = 1 AND ISNULL(tf.CandidateRatingPass, 0) = 1
                     AND ISNULL(tf.JoinTimePass, 0) = 1 AND ISNULL(tf.InterviewDurationPass, 0) = 1
                    THEN 5 ELSE 4
                END AS SeveritySort
            FROM dbo.TrialFlags tf
            LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
            LEFT JOIN dbo.IRF irf ON irf.id = tf.IrfId
            LEFT JOIN dbo.UserList ulco ON ulco.id = irf.RisebirdHr
            LEFT JOIN dbo.InterviewMeetings im ON im.id = tf.TriggeringInterviewId
            LEFT JOIN dbo.TransInterviewer ti ON ti.id = tf.ChannelId
            LEFT JOIN dbo.Interviewer iv ON iv.id = COALESCE(ti.InterviewerId, tf.InterviewerId)
            LEFT JOIN dbo.UserList ulIv ON ulIv.id = iv.UserId
            LEFT JOIN dbo.InterviewerProfile ipTi ON ipTi.id = COALESCE(ti.InterviewerId, tf.InterviewerId)
            LEFT JOIN dbo.InterviewerProfile ipUl ON ipUl.UserId = iv.UserId
            WHERE (@StatusTrim IS NULL OR LOWER(LTRIM(RTRIM(ISNULL(tf.ReviewStatus, '')))) = @StatusTrim)
              AND (
                    @OwnerTrim IS NULL
                 OR LOWER(LTRIM(RTRIM(ISNULL(ulco.EmailId, '')))) = LOWER(@OwnerTrim)
              )
              AND (
                    @SearchTrim IS NULL
                 OR COALESCE(irf.IRFId, im.IRFId, '') LIKE N'%' + @SearchTrim + N'%'
                 OR COALESCE(ulco.EmailId, '') LIKE N'%' + @SearchTrim + N'%'
                 OR COALESCE(ulco.FirstName, '') LIKE N'%' + @SearchTrim + N'%'
                 OR COALESCE(ulco.LastName, '') LIKE N'%' + @SearchTrim + N'%'
                 OR COALESCE(ulIv.EmailId, im.interviewer, '') LIKE N'%' + @SearchTrim + N'%'
                 OR COALESCE(ipTi.NickName, ipUl.NickName, '') LIKE N'%' + @SearchTrim + N'%'
                 OR CAST(tf.TriggeringInterviewId AS VARCHAR(20)) LIKE N'%' + @SearchTrim + N'%'
              )
        ),
        Filtered AS (
            SELECT *, COUNT(1) OVER() AS TotalRows
            FROM Ranked
            WHERE (@SeverityTrim IS NULL OR UPPER(LTRIM(RTRIM(Severity))) = @SeverityTrim)
        )
        INSERT INTO #TfrPage (
            TrialFlagId, InterviewerId, IrfId, ChannelId, TriggeringInterviewId, TriggeringInterviewDate,
            OverallVerdict, ReviewStatus, FlaggedAtUtc, ReviewedAtUtc, ReviewedBy, ReviewerComment,
            TranscriptQualityPass, ReportQualityPass, DecisionQualityPass,
            CandidateRatingPass, JoinTimePass, InterviewDurationPass,
            Severity, SeveritySort, TotalRows)
        SELECT
            TrialFlagId, InterviewerId, IrfId, ChannelId, TriggeringInterviewId, TriggeringInterviewDate,
            OverallVerdict, ReviewStatus, FlaggedAtUtc, ReviewedAtUtc, ReviewedBy, ReviewerComment,
            TranscriptQualityPass, ReportQualityPass, DecisionQualityPass,
            CandidateRatingPass, JoinTimePass, InterviewDurationPass,
            Severity, SeveritySort, TotalRows
        FROM Filtered
        ORDER BY SeveritySort ASC, FlaggedAtUtc DESC, TrialFlagId DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
    END;

    IF NOT EXISTS (SELECT 1 FROM #TfrPage)
    BEGIN
        SELECT TOP (0)
            CAST(0 AS INT) AS TrialFlagId, CAST(0 AS INT) AS InterviewerId, CAST(0 AS INT) AS IrfId,
            CAST(0 AS INT) AS ChannelId, CAST(0 AS INT) AS TriggeringInterviewId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST('' AS VARCHAR(30)) AS OverallVerdict, CAST('' AS VARCHAR(30)) AS DisplayVerdict,
            CAST(NULL AS VARCHAR(4)) AS Severity, CAST('' AS VARCHAR(20)) AS ReviewStatus,
            CAST(NULL AS DATETIME) AS FlaggedAtUtc, CAST(NULL AS DATETIME) AS ReviewedAtUtc,
            CAST(NULL AS VARCHAR(150)) AS ReviewedBy, CAST(NULL AS NVARCHAR(500)) AS ReviewerComment,
            CAST('' AS NVARCHAR(200)) AS InterviewerName, CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId, CAST(NULL AS INT) AS InterviewerUserId,
            CAST(NULL AS VARCHAR(20)) AS InterviewerMobileNumber,
            CAST(NULL AS NVARCHAR(200)) AS InterviewerAltMobileNumber, CAST(0 AS BIT) AS IsSuspicious,
            CAST('' AS NVARCHAR(200)) AS IrfName, CAST(NULL AS VARCHAR(200)) AS ChannelOwnerEmail,
            CAST(NULL AS NVARCHAR(300)) AS ChannelOwner, CAST(NULL AS VARCHAR(MAX)) AS RiskCategoryLabels,
            CAST(NULL AS VARCHAR(500)) AS RiskCategoryReasons, CAST(NULL AS VARCHAR(20)) AS QcMergeVerdict,
            CAST(NULL AS INT) AS QcMergeTotalPenalty, CAST(NULL AS INT) AS QcMergeQcScore,
            CAST(NULL AS INT) AS QcMergeScore, CAST(0 AS INT) AS TotalRows;
        DROP TABLE #TfrPage;
        RETURN;
    END;

    /* -------- Stage 2: enrich paged rows only (≤ @PageSize) -------- */
    SELECT
        p.TrialFlagId, p.InterviewerId, p.IrfId, p.ChannelId, p.TriggeringInterviewId, p.TriggeringInterviewDate,
        p.OverallVerdict,
        CASE
            WHEN rc.Severity IS NOT NULL THEN 'bad_trial'
            WHEN ISNULL(p.TranscriptQualityPass, 0) = 1 AND ISNULL(p.ReportQualityPass, 0) = 1
             AND ISNULL(p.DecisionQualityPass, 0) = 1 AND ISNULL(p.CandidateRatingPass, 0) = 1
             AND ISNULL(p.JoinTimePass, 0) = 1 AND ISNULL(p.InterviewDurationPass, 0) = 1
            THEN 'good_trial' ELSE 'bad_trial'
        END AS DisplayVerdict,
        p.Severity, p.ReviewStatus, p.FlaggedAtUtc, p.ReviewedAtUtc, p.ReviewedBy, p.ReviewerComment,
        COALESCE(
            NULLIF(LTRIM(RTRIM(ru.EmailId)), ''),
            NULLIF(LTRIM(RTRIM(ulViaInterviewer.EmailId)), ''),
            NULLIF(LTRIM(RTRIM(im.interviewer)), ''),
            'Interviewer #' + CAST(p.InterviewerId AS VARCHAR(12))
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
            CASE WHEN p.IrfId > 0 THEN 'IRF #' + CAST(p.IrfId AS VARCHAR(12)) ELSE '' END
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
        p.TotalRows
    FROM #TfrPage p
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = p.TriggeringInterviewId
    LEFT JOIN dbo.TransInterviewer ti ON ti.id = p.ChannelId
    LEFT JOIN dbo.IRF irf ON irf.id = p.IrfId
    LEFT JOIN dbo.UserList ulco ON ulco.id = irf.RisebirdHr
    LEFT JOIN dbo.InterviewMeetings im ON im.id = p.TriggeringInterviewId
    LEFT JOIN dbo.IRF irfViaInterview ON irfViaInterview.IRFId = im.IRFId
    LEFT JOIN dbo.Interviewer iv ON iv.id = COALESCE(ti.InterviewerId, p.InterviewerId)
    LEFT JOIN dbo.InterviewerProfile ipViaTi ON ipViaTi.id = COALESCE(ti.InterviewerId, p.InterviewerId)
    LEFT JOIN dbo.UserList ulViaInterviewer ON ulViaInterviewer.id = iv.UserId
    LEFT JOIN dbo.InterviewerProfile ipViaInterviewer ON ipViaInterviewer.UserId = iv.UserId
    OUTER APPLY (
        SELECT TOP 1
            qq.InterviewId, qq.qcmergeverdict, qq.qcmergescore,
            qq.AIMergeRedoInterview, qq.AIMergeInterviewerSuspicious,
            CASE WHEN @HasAiMergePenalty = 1 THEN qq.AIMergeTotalPenalty END AS AIMergeTotalPenalty
        FROM dbo.QcInterviewAIevaluation qq
        WHERE qq.InterviewId = p.TriggeringInterviewId
        ORDER BY qq.id DESC
    ) q
    OUTER APPLY (
        SELECT TOP 1 x.UserId, x.EmailId, x.MobileNumber
        FROM (
            SELECT ul.id AS UserId, ul.EmailId, ul.MobileNumber, 1 AS Priority
            FROM dbo.UserList ul
            WHERE im.interviewer IS NOT NULL AND LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(im.interviewer))
            UNION ALL
            SELECT ul.id, ul.EmailId, ul.MobileNumber, 2
            FROM dbo.UserList ul
            WHERE im.interviewer IS NOT NULL AND ul.EmailId = im.interviewer
            UNION ALL
            SELECT ip.UserId, ul.EmailId, ul.MobileNumber, 3
            FROM dbo.InterviewerProfile ip
            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
            WHERE im.interviewer IS NOT NULL AND LTRIM(RTRIM(ip.NickName)) = LTRIM(RTRIM(im.interviewer))
            UNION ALL
            SELECT ul.id, ul.EmailId, ul.MobileNumber, 4
            FROM dbo.UserList ul WHERE ti.UserId IS NOT NULL AND ul.id = ti.UserId
            UNION ALL
            SELECT ul.id, ul.EmailId, ul.MobileNumber, 5
            FROM dbo.UserList ul WHERE iv.UserId IS NOT NULL AND ul.id = iv.UserId
            UNION ALL
            SELECT ul.id, ul.EmailId, ul.MobileNumber, 6
            FROM dbo.UserList ul WHERE ipViaTi.UserId IS NOT NULL AND ul.id = ipViaTi.UserId
        ) x
        WHERE NULLIF(LTRIM(RTRIM(x.EmailId)), '') IS NOT NULL OR x.UserId IS NOT NULL
        ORDER BY x.Priority
    ) ru
    LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ru.UserId
    LEFT JOIN dbo.SuspiciousInterviewer si ON si.UserId = ru.UserId AND ISNULL(si.SuspiciousStatus, 0) = 1
    ORDER BY p.SeveritySort ASC, p.FlaggedAtUtc DESC, p.TrialFlagId DESC;

    DROP TABLE #TfrPage;
END
GO
