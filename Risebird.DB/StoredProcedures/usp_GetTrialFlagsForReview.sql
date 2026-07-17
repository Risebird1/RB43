CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagsForReview]
    @ReviewStatus VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @HasMergedJson BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasAiMergePenalty BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'AIMergeTotalPenalty') IS NOT NULL THEN 1 ELSE 0 END;

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
            CAST(NULL AS DECIMAL(5,2)) AS ReportQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS CandidateRating,
            CAST(NULL AS DECIMAL(8,2)) AS JoinTimeMinutes,
            CAST(NULL AS DECIMAL(8,2)) AS InterviewDurationMinutes,
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
            CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(NULL AS VARCHAR(MAX)) AS RiskCategoryLabels,
            CAST(NULL AS VARCHAR(500)) AS RiskCategoryReasons,
            CAST(NULL AS VARCHAR(500)) AS SeverityBreakdown,
            CAST(NULL AS NVARCHAR(1000)) AS CandidateFeedbackComment,
            CAST(NULL AS VARCHAR(20)) AS QcMergeVerdict,
            CAST(NULL AS INT) AS QcMergeTotalPenalty,
            CAST(NULL AS INT) AS QcMergeQcScore,
            CAST(NULL AS INT) AS QcMergeScore,
            CAST(NULL AS NVARCHAR(MAX)) AS QcMergeFlagsJson,
            CAST(0 AS INT) AS PaymentDeduction,
            CAST(NULL AS NVARCHAR(MAX)) AS PaymentReasonValuesJson,
            CAST('' AS VARCHAR(500)) AS PaymentDeductionReasons,
            CAST(NULL AS VARCHAR(15)) AS PaymentRecommendation,
            CAST(NULL AS VARCHAR(500)) AS PaymentRecommendationComments
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
        COALESCE(tf.TranscriptQualityScore, CAST(NULLIF(ic.AITranscriptAnalysisScore, 0) AS DECIMAL(5,2))) AS TranscriptQualityScore,
        CAST(NULLIF(ic.AIReportQCAnalysisScore, 0) AS DECIMAL(5,2)) AS ReportQualityScore,
        COALESCE(tf.CandidateRating, CAST(NULLIF(cfd.Rating, 0) AS DECIMAL(5,2))) AS CandidateRating,
        tf.JoinTimeMinutes,
        tf.InterviewDurationMinutes,
        tf.OverallVerdict,
        -- DisplayVerdict: live risk only when rc.Severity IS NOT NULL (husk/NULL row is NOT risk).
        -- Otherwise factor-based: all six Pass bits = good_trial, else bad_trial. Never trust stale tf alone for risk.
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
        -- DisplaySeverity: live rc.Severity when present; else P4 (factor fail) / P5 (clean).
        -- NEVER fall back to tf.Severity; NEVER default NULL to P0.
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
        COALESCE(
            NULLIF(LTRIM(RTRIM(irf.IRFId)), ''),
            NULLIF(LTRIM(RTRIM(irfViaInterview.IRFId)), ''),
            NULLIF(LTRIM(RTRIM(im.IRFId)), ''),
            CASE WHEN tf.IrfId > 0 THEN 'IRF #' + CAST(tf.IrfId AS VARCHAR(12)) ELSE '' END
        ) AS IrfName,
        rc.Name AS RiskCategoryLabels,
        rc.Description AS RiskCategoryReasons,
        rc.SeverityBreakdown,
        ISNULL(STUFF(CONCAT(
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.CandidateInteractionQuality)), '') IS NOT NULL THEN ' · ' + LTRIM(RTRIM(cfd.CandidateInteractionQuality)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.LimitedHandoncoding)), '') IS NOT NULL THEN ' · ' + LTRIM(RTRIM(cfd.LimitedHandoncoding)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.WeakInfrastructure)), '') IS NOT NULL THEN ' · ' + LTRIM(RTRIM(cfd.WeakInfrastructure)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.UnethicalPractices)), '') IS NOT NULL THEN ' · ' + LTRIM(RTRIM(cfd.UnethicalPractices)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.TimeManagement)), '') IS NOT NULL THEN ' · ' + LTRIM(RTRIM(cfd.TimeManagement)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.PoorCommunicationSkills)), '') IS NOT NULL THEN ' · ' + LTRIM(RTRIM(cfd.PoorCommunicationSkills)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.InterviewerVideo)), '') IS NOT NULL THEN ' · ' + LTRIM(RTRIM(cfd.InterviewerVideo)) ELSE '' END
        ), 1, 3, ''), '') AS CandidateFeedbackComment,
        CASE
            WHEN q.InterviewId IS NULL THEN NULL
            WHEN ISNULL(q.AIMergeRedoInterview, 0) = 1
              OR (
                    @HasMergedJson = 1
                AND ISNULL(TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1
                 ) THEN 'redo'
            WHEN UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) IN ('YES', 'Y', 'TRUE') THEN 'suspicious'
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(q.qcmergeverdict, ''))), '') IS NOT NULL THEN LTRIM(RTRIM(q.qcmergeverdict))
            ELSE 'ok'
        END AS QcMergeVerdict,
        COALESCE(
            CASE WHEN @HasAiMergePenalty = 1 THEN q.AIMergeTotalPenalty END,
            CASE WHEN @HasMergedJson = 1 THEN TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.total_penalty') AS INT) END
        ) AS QcMergeTotalPenalty,
        COALESCE(
            q.qcmergescore,
            CASE WHEN @HasMergedJson = 1 THEN TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.total_qcmerge_score') AS INT) END
        ) AS QcMergeQcScore,
        -- Legacy: same as Total Penalty (do not conflate with QC score)
        COALESCE(
            CASE WHEN @HasAiMergePenalty = 1 THEN q.AIMergeTotalPenalty END,
            CASE WHEN @HasMergedJson = 1 THEN TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.total_penalty') AS INT) END
        ) AS QcMergeScore,
        CASE WHEN @HasMergedJson = 1 THEN JSON_QUERY(q.MergedAIQCDetails, '$.flags') END AS QcMergeFlagsJson,
        ISNULL(ps.PaymentDeduction, 0) AS PaymentDeduction,
        (SELECT
            ps.LimitedHandoncoding AS LimitedHandoncoding,
            ps.WrongStatusUpdates AS WrongStatusUpdates,
            ps.WeakInfrastructure AS WeakInfrastructure,
            ps.ProctoringIgnorance AS ProctoringIgnorance,
            ps.UnethicalPractices AS UnethicalPractices,
            ps.PoorCommunicationSkills AS PoorCommunicationSkills,
            ps.AttendanceJoinCompliance AS AttendanceJoinCompliance
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS PaymentReasonValuesJson,
        -- Legacy label CSV (render path prefers PaymentReasonValuesJson + TrialFlagEvidenceBadgeHelper)
        ISNULL(STUFF(CONCAT(
            CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(ps.LimitedHandoncoding, '')))) NOT IN ('', '0', 'NO', 'N', 'FALSE', 'N/A', 'NA', 'NONE') THEN ',LimitedHandoncoding' ELSE '' END,
            CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(ps.WrongStatusUpdates, '')))) NOT IN ('', '0', 'NO', 'N', 'FALSE', 'N/A', 'NA', 'NONE') THEN ',WrongStatusUpdates' ELSE '' END,
            CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(ps.WeakInfrastructure, '')))) NOT IN ('', '0', 'NO', 'N', 'FALSE', 'N/A', 'NA', 'NONE') THEN ',WeakInfrastructure' ELSE '' END,
            CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(ps.ProctoringIgnorance, '')))) NOT IN ('', '0', 'NO', 'N', 'FALSE', 'N/A', 'NA', 'NONE') THEN ',ProctoringIgnorance' ELSE '' END,
            CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(ps.UnethicalPractices, '')))) NOT IN ('', '0', 'NO', 'N', 'FALSE', 'N/A', 'NA', 'NONE') THEN ',UnethicalPractices' ELSE '' END,
            CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(ps.PoorCommunicationSkills, '')))) NOT IN ('', '0', 'NO', 'N', 'FALSE', 'N/A', 'NA', 'NONE') THEN ',PoorCommunicationSkills' ELSE '' END,
            CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(ps.AttendanceJoinCompliance, '')))) NOT IN ('', '0', 'NO', 'N', 'FALSE', 'N/A', 'NA', 'NONE') THEN ',AttendanceJoinCompliance' ELSE '' END
        ), 1, 1, ''), '') AS PaymentDeductionReasons,
        ps.Recommendation AS PaymentRecommendation,
        ps.RecommendationComments AS PaymentRecommendationComments
    FROM dbo.TrialFlags tf
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
    LEFT JOIN dbo.InterviewComments ic ON ic.InterviewId = tf.TriggeringInterviewId
    OUTER APPLY (
        SELECT TOP 1 *
        FROM dbo.CandidateFeedbackDetails x
        WHERE x.InterviewId = tf.TriggeringInterviewId
        ORDER BY x.Id DESC
    ) cfd
    OUTER APPLY (
        SELECT TOP 1 qq.*
        FROM dbo.QcInterviewAIevaluation qq
        WHERE qq.InterviewId = tf.TriggeringInterviewId
        ORDER BY qq.id DESC
    ) q
    OUTER APPLY (
        SELECT TOP 1 pp.*
        FROM dbo.PaymentSLADetails pp
        WHERE pp.Interviewid = tf.TriggeringInterviewId
        ORDER BY pp.id DESC
    ) ps
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
