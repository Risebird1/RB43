/*
================================================================================
usp_GetTrialFlagDetail — TIER 2 on-demand row detail (single TrialFlagId)

MAINTAINER RULE:
  Heavy per-row work lives HERE: ISJSON-guarded JSON_VALUE on MergedAIQCDetails,
  PaymentSLADetails, CandidateFeedbackDetails, InterviewComments scores,
  SeverityBreakdown, pass bits + raw metrics.

  Do NOT call this from list loads. Grid expand / keep-remove refresh only.

  Do NOT raise the default command timeout — one interview should stay under 1s.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagDetail]
    @TrialFlagId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Prefer row versions over blocking writers when RCS is enabled (Part 2).
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    IF @TrialFlagId IS NULL OR @TrialFlagId <= 0
        RETURN;

    DECLARE @HasMergedJson BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasAiMergePenalty BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'AIMergeTotalPenalty') IS NOT NULL THEN 1 ELSE 0 END;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
        RETURN;

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
                AND ISJSON(q.MergedAIQCDetails) = 1
                AND ISNULL(TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1
                 ) THEN 'redo'
            WHEN UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) IN ('YES', 'Y', 'TRUE') THEN 'suspicious'
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(q.qcmergeverdict, ''))), '') IS NOT NULL THEN LTRIM(RTRIM(q.qcmergeverdict))
            ELSE 'ok'
        END AS QcMergeVerdict,
        COALESCE(
            CASE WHEN @HasAiMergePenalty = 1 THEN q.AIMergeTotalPenalty END,
            CASE WHEN @HasMergedJson = 1 AND ISJSON(q.MergedAIQCDetails) = 1
                 THEN TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.total_penalty') AS INT) END
        ) AS QcMergeTotalPenalty,
        COALESCE(
            q.qcmergescore,
            CASE WHEN @HasMergedJson = 1 AND ISJSON(q.MergedAIQCDetails) = 1
                 THEN TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.total_qcmerge_score') AS INT) END
        ) AS QcMergeQcScore,
        COALESCE(
            CASE WHEN @HasAiMergePenalty = 1 THEN q.AIMergeTotalPenalty END,
            CASE WHEN @HasMergedJson = 1 AND ISJSON(q.MergedAIQCDetails) = 1
                 THEN TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.total_penalty') AS INT) END
        ) AS QcMergeScore,
        CASE
            WHEN @HasMergedJson = 1 AND ISJSON(q.MergedAIQCDetails) = 1
            THEN JSON_QUERY(q.MergedAIQCDetails, '$.flags')
            ELSE NULL
        END AS QcMergeFlagsJson,
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
    WHERE tf.Id = @TrialFlagId;
END
GO
