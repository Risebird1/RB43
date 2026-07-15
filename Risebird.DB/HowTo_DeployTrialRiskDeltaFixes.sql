-- Deploy all updated SPs for trial-risk-delta-fixes (ul.id late-join fix, FeedBackComment removed, clean regrade DELETE).
-- Run against the target DB in SSMS. Then run HowTo_BackfillRiskCategory_Last90Days.sql.

PRINT '1/4 usp_AddUpdateRiskCategory...';
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_AddUpdateRiskCategory]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Tunable thresholds (named constants)
    DECLARE @DeficitMoreThan10_Min INT = 10;
    DECLARE @Deficit_P2_Max INT = 20;
    DECLARE @Deficit_P1_Max INT = 30;
    DECLARE @TranscriptLt4_P2_Min INT = 3;
    DECLARE @TranscriptLt4_P1_Min INT = 2;
    DECLARE @RatingLt4_P2_Min INT = 3;
    DECLARE @RatingLt4_P1_Min INT = 2;
    DECLARE @SelectDeficit_Ratio_P3_Min DECIMAL(10,4) = 0.5;
    DECLARE @SelectDeficit_Ratio_P2_Min DECIMAL(10,4) = 0.333333;
    DECLARE @SelectDeficit_Ratio_P1_Max DECIMAL(10,4) = 0.333333;
    DECLARE @RejectDeficit_Ratio_P4_Min DECIMAL(10,4) = 0.333333;
    DECLARE @RejectDeficit_Ratio_P3_Min DECIMAL(10,4) = 0.222222;
    DECLARE @RejectDeficit_Ratio_P2_Max DECIMAL(10,4) = 0.222222;
    DECLARE @ReportScoreLt60_P2_Max INT = 60;
    DECLARE @ReportScoreLt60_P1_Max INT = 40;
    DECLARE @ReportScoreLt60_P0_Max INT = 20;
    DECLARE @TranscriptLt6_P4_Min INT = 5;
    DECLARE @TranscriptLt6_P3_Min INT = 4;
    DECLARE @TranscriptLt6_P2_Min INT = 3;
    DECLARE @TranscriptLt6_P1_Min INT = 2;
    DECLARE @Contradiction_MaxScore INT = 5;
    -- #13 InterviewerLateJoin bands (minutes)
    DECLARE @LateJoin_P2_Min INT = 5;   -- grade when j > 5
    DECLARE @LateJoin_P1_Min INT = 10;  -- j > 10 â†’ P1; 5 < j <= 10 â†’ P2

    DECLARE @HasMergedJson BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;

    DECLARE @Name               VARCHAR(MAX);
    DECLARE @Description        VARCHAR(MAX);
    DECLARE @Severity           VARCHAR(4);
    DECLARE @SeverityBreakdown  VARCHAR(500);
    DECLARE @MinPLevel          INT;

    ;WITH Checks AS (
        -- #1 deficitmorethan10
        SELECT im.id AS InterviewId, 'deficitmorethan10' AS Type, 'High Severity' AS Riskcategory,
               CASE WHEN (pt.DurationSelectedMin - ic.VideoDuration) > @Deficit_P1_Max THEN 0
                    WHEN (pt.DurationSelectedMin - ic.VideoDuration) > @Deficit_P2_Max THEN 1 ELSE 2 END AS PLevel
        FROM InterviewMeetings im
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry
             AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType
        WHERE im.id = @InterviewId AND ic.MeetingStatus = 4
          AND (ic.VideoDuration - pt.DurationSelectedMin) > (-pt.DurationSelectedMin)
          AND (ic.VideoDuration - pt.DurationSelectedMin) < -@DeficitMoreThan10_Min

        UNION ALL
        -- #2 AIVerdictLessthan4
        SELECT im.id, 'AIVerdictLessthan4', 'High Severity',
               CASE WHEN NULLIF(ic.AITranscriptAnalysisScore,0) < @TranscriptLt4_P1_Min THEN 0
                    WHEN NULLIF(ic.AITranscriptAnalysisScore,0) < @TranscriptLt4_P2_Min THEN 1 ELSE 2 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND NULLIF(ic.AITranscriptAnalysisScore,0) < 4

        UNION ALL
        -- #3 CandidateRating
        SELECT im.id, 'CandidateRating', 'High Severity',
               CASE WHEN cfd.Rating < @RatingLt4_P1_Min THEN 0
                    WHEN cfd.Rating < @RatingLt4_P2_Min THEN 1 ELSE 2 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN CandidateFeedbackDetails cfd ON cfd.InterviewId = im.id
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND cfd.Rating < 4

        UNION ALL
        -- #4 Selectdeficit
        SELECT im.id, 'Selectdeficit', 'Under Review',
               CASE WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @SelectDeficit_Ratio_P1_Max THEN 1
                    WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @SelectDeficit_Ratio_P3_Min THEN 2 ELSE 3 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry
             AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND pt.DurationSelectedMin > 0
          AND (pt.DurationSelectedMin * 2 / 3 > ic.VideoDuration) AND ic.InterviewStatus IN (2,4)

        UNION ALL
        -- #5 Rejectdeficit
        SELECT im.id, 'Rejectdeficit', 'Under Review',
               CASE WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @RejectDeficit_Ratio_P2_Max THEN 2
                    WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @RejectDeficit_Ratio_P4_Min THEN 3 ELSE 4 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry
             AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND pt.DurationSelectedMin > 0
          AND (pt.DurationSelectedMin * 4 / 9 > ic.VideoDuration) AND ic.InterviewStatus IN (1,3,5)

        UNION ALL
        -- #6 AIRedo â€” dual source: bit column OR MergedAIQCDetails JSON (same pattern as IRF audit directory)
        SELECT qca.InterviewId, 'AIRedo', 'Redo Requested', 0
        FROM QcInterviewAIevaluation qca
        WHERE qca.InterviewId = @InterviewId
          AND (
                ISNULL(qca.AIMergeRedoInterview, 0) = 1
             OR (
                    @HasMergedJson = 1
                AND ISNULL(TRY_CAST(JSON_VALUE(qca.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1
                )
              )

        UNION ALL
        -- #7 Report AI Score
        SELECT InterviewId, 'Report AI Score', 'High Severity',
               CASE WHEN NULLIF(AIReportQCAnalysisScore,0) <= @ReportScoreLt60_P0_Max THEN 0
                    WHEN NULLIF(AIReportQCAnalysisScore,0) <= @ReportScoreLt60_P1_Max THEN 1 ELSE 2 END
        FROM InterviewComments
        WHERE InterviewId = @InterviewId AND NULLIF(AIReportQCAnalysisScore,0) < @ReportScoreLt60_P2_Max

        UNION ALL
        -- #8 Transcript AI Score
        SELECT InterviewId, 'Transcript AI Score', 'High Severity',
               CASE WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P1_Min THEN 0
                    WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P2_Min THEN 1
                    WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P3_Min THEN 2
                    WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P4_Min THEN 3 ELSE 4 END
        FROM InterviewComments
        WHERE InterviewId = @InterviewId AND NULLIF(AITranscriptAnalysisScore,0) < 6

        UNION ALL
        -- #9 Contradiction
        SELECT qca.InterviewId, 'Contradiction AI Score', 'High Severity',
               CASE CAST(TRY_CAST(qca.ContradictionScore AS DECIMAL(10,2)) AS INT)
                    WHEN 1 THEN 0 WHEN 2 THEN 1 WHEN 3 THEN 2 WHEN 4 THEN 3 WHEN 5 THEN 4 END
        FROM QcInterviewAIevaluation qca
        WHERE qca.InterviewId = @InterviewId
          AND TRY_CAST(qca.ContradictionScore AS DECIMAL(10,2)) BETWEEN 1 AND @Contradiction_MaxScore

        UNION ALL
        -- #10 No-show
        SELECT im.id,
               CASE WHEN imj.Joinedon IS NULL AND imc.PerformedOn IS NULL THEN 'No Show'
                    WHEN imj.Joinedon IS NULL THEN 'Interviewer No Show'
                    ELSE 'Candidate No Show' END,
               'Fake Report', 0
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        OUTER APPLY (SELECT MAX(Joinedon) AS Joinedon FROM InterviewMeetingsJoinTime WHERE Interviewid = im.id) imj
        OUTER APPLY (SELECT MAX(PerformedOn) AS PerformedOn FROM PageClickDetails
                     WHERE ButtonAction='JoinMeeting' AND Interviewid=im.id AND ProfileType='Candidate') imc
        WHERE im.id = @InterviewId
          AND im.confirmationStatus = 1
          AND im.ZoomMeetingId IN (SELECT DISTINCT Meetingid FROM ZoomMeetingTransmission)
          AND (imj.Joinedon IS NULL OR imc.PerformedOn IS NULL)
          AND ic.MeetingStatus NOT IN (1,2,3,5,9)

        UNION ALL
        -- #11 InterviewerVideoOff (PaymentSLADetails) â†’ P0 always
        SELECT im.id, 'InterviewerVideoOff', 'Fake Report', 0
        FROM InterviewMeetings im
        INNER JOIN PaymentSLADetails ps ON ps.Interviewid = im.id
        WHERE im.id = @InterviewId
          AND (
                ISNULL(ps.VideoEnable, 1) = 0
             OR ISNULL(ps.InterviewerVideo, '') LIKE '%Video Off%'
              )

        UNION ALL
        -- #12 CandidateRedoRequest â†’ P0 always (distinct from #6 AI redo)
        SELECT im.id, 'CandidateRedoRequest', 'Redo Requested', 0
        FROM InterviewMeetings im
        WHERE im.id = @InterviewId
          AND EXISTS (
                SELECT 1 FROM RedoInterviews ri
                WHERE ri.interviewid = im.id
                  AND ISNULL(ri.Rejected, 0) = 0
              )

        UNION ALL
        -- #13 InterviewerLateJoin â€” graded; NULL/unmatched join = not triggered
        SELECT im.id, 'InterviewerLateJoin',
               CASE WHEN late.JoinMinutes > @LateJoin_P1_Min THEN 'High Severity' ELSE 'Under Review' END,
               CASE WHEN late.JoinMinutes > @LateJoin_P1_Min THEN 1 ELSE 2 END
        FROM InterviewMeetings im
        CROSS APPLY (
            SELECT TOP 1
                CASE
                    WHEN DATEDIFF(MINUTE, im.interviewDate, j.Joinedon) < 0 THEN 0
                    ELSE DATEDIFF(MINUTE, im.interviewDate, j.Joinedon)
                END AS JoinMinutes
            FROM InterviewMeetingsJoinTime j
            WHERE j.Interviewid = im.id
              AND j.Joinedon IS NOT NULL
              AND (
                    LTRIM(RTRIM(ISNULL(j.interviewer, ''))) = LTRIM(RTRIM(ISNULL(im.interviewer, '')))
                 OR EXISTS (
                        SELECT 1
                        FROM UserList ul
                        LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id
                        WHERE (
                                LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(im.interviewer))
                             OR LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = LTRIM(RTRIM(im.interviewer))
                              )
                          AND (
                                LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(j.interviewer))
                             OR LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = LTRIM(RTRIM(j.interviewer))
                              )
                    )
                  )
            ORDER BY j.Joinedon ASC
        ) late
        WHERE im.id = @InterviewId
          AND late.JoinMinutes > @LateJoin_P2_Min
    )
    SELECT
        @Name              = STRING_AGG(x.Riskcategory, ','),
        @Description       = STRING_AGG(x.Type, ','),
        @SeverityBreakdown = STRING_AGG(x.Type + ':P' + CAST(x.PLevel AS VARCHAR(1)), ','),
        @MinPLevel         = MIN(x.PLevel)
    FROM Checks x
    WHERE x.InterviewId = @InterviewId
      AND x.PLevel IS NOT NULL;

    -- Overall severity = MIN(PLevel); P0 = highest severity â€¦ P5 = clean
    IF @MinPLevel IS NOT NULL
        SET @Severity = 'P' + CAST(@MinPLevel AS VARCHAR(1));

    IF ISNULL(LTRIM(RTRIM(@Description)), '') = ''
    BEGIN
        -- No checks triggered: remove stale RiskCategory row (do not leave NULL severity)
        DELETE FROM RiskCategory WHERE InterviewId = @InterviewId;
    END
    ELSE IF EXISTS (SELECT 1 FROM RiskCategory WHERE InterviewId = @InterviewId)
        UPDATE RiskCategory SET Name=@Name, Description=@Description,
               Severity=@Severity, SeverityBreakdown=@SeverityBreakdown, ModifiedOn=GETDATE()
        WHERE InterviewId = @InterviewId;
    ELSE
        INSERT INTO RiskCategory (InterviewId, Name, Description, Severity, SeverityBreakdown, CreatedOn)
        VALUES (@InterviewId, @Name, @Description, @Severity, @SeverityBreakdown, GETDATE());
END
GO

GO

PRINT '2/4 usp_GetInterviewAiRiskSummary...';
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewAiRiskSummary]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReportScoreLt60_P2_Max INT = 60;
    DECLARE @ReportScoreLt60_P1_Max INT = 40;
    DECLARE @ReportScoreLt60_P0_Max INT = 20;
    DECLARE @TranscriptLt4_P2_Min INT = 3;
    DECLARE @TranscriptLt4_P1_Min INT = 2;
    DECLARE @TranscriptLt6_P4_Min INT = 5;
    DECLARE @TranscriptLt6_P3_Min INT = 4;
    DECLARE @TranscriptLt6_P2_Min INT = 3;
    DECLARE @TranscriptLt6_P1_Min INT = 2;
    DECLARE @Contradiction_MaxScore INT = 5;

    DECLARE @HasMergedJson BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;

    DECLARE @ReportScore DECIMAL(10,2) = NULL;
    DECLARE @TranscriptScore DECIMAL(10,2) = NULL;
    DECLARE @ContradictionScore DECIMAL(10,2) = NULL;
    DECLARE @RedoBit BIT = 0;
    DECLARE @Suspicious VARCHAR(20) = NULL;

    SELECT
        @ReportScore = CAST(NULLIF(ic.AIReportQCAnalysisScore, 0) AS DECIMAL(10,2)),
        @TranscriptScore = CAST(NULLIF(ic.AITranscriptAnalysisScore, 0) AS DECIMAL(10,2))
    FROM dbo.InterviewComments ic
    WHERE ic.InterviewId = @InterviewId;

    SELECT
        @ContradictionScore = TRY_CAST(qca.ContradictionScore AS DECIMAL(10,2)),
        @RedoBit = CASE
            WHEN ISNULL(qca.AIMergeRedoInterview, 0) = 1 THEN 1
            WHEN @HasMergedJson = 1
             AND ISNULL(TRY_CAST(JSON_VALUE(qca.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1 THEN 1
            ELSE 0 END,
        @Suspicious = COALESCE(
            NULLIF(LTRIM(RTRIM(qca.AIMergeInterviewerSuspicious)), ''),
            CASE WHEN @HasMergedJson = 1
                 THEN NULLIF(LTRIM(RTRIM(JSON_VALUE(qca.MergedAIQCDetails, '$.interviewer_suspicious'))), '')
                 ELSE NULL END
        )
    FROM dbo.QcInterviewAIevaluation qca
    WHERE qca.InterviewId = @InterviewId;

    -- Report AI (#7)
    SELECT
        CAST('Report AI' AS VARCHAR(40)) AS SourceName,
        CAST(@ReportScore AS VARCHAR(40)) AS RawScore,
        CAST(CASE WHEN @ReportScore IS NOT NULL AND @ReportScore < @ReportScoreLt60_P2_Max THEN 1 ELSE 0 END AS BIT) AS Triggered,
        CAST(CASE
            WHEN @ReportScore IS NULL OR @ReportScore >= @ReportScoreLt60_P2_Max THEN NULL
            WHEN @ReportScore <= @ReportScoreLt60_P0_Max THEN 'P0'
            WHEN @ReportScore <= @ReportScoreLt60_P1_Max THEN 'P1'
            ELSE 'P2'
        END AS VARCHAR(4)) AS PLevel

    UNION ALL
    -- Transcript AI (#2 and #8 â†’ worst P of the two)
    SELECT
        'Transcript AI',
        CAST(@TranscriptScore AS VARCHAR(40)),
        CAST(CASE WHEN @TranscriptScore IS NOT NULL AND @TranscriptScore < 6 THEN 1 ELSE 0 END AS BIT),
        CAST(CASE
            WHEN @TranscriptScore IS NULL OR @TranscriptScore >= 6 THEN NULL
            WHEN @TranscriptScore < 4 THEN
                -- min of #2 and #8 for scores < 4
                CASE
                    WHEN @TranscriptScore < @TranscriptLt4_P1_Min THEN 'P0'  -- both point to 0
                    WHEN @TranscriptScore < @TranscriptLt4_P2_Min THEN 'P1'
                    ELSE 'P2'
                END
            WHEN @TranscriptScore < @TranscriptLt6_P1_Min THEN 'P0'
            WHEN @TranscriptScore < @TranscriptLt6_P2_Min THEN 'P1'
            WHEN @TranscriptScore < @TranscriptLt6_P3_Min THEN 'P2'
            WHEN @TranscriptScore < @TranscriptLt6_P4_Min THEN 'P3'
            ELSE 'P4'
        END AS VARCHAR(4))

    UNION ALL
    -- Contradiction AI (#9)
    SELECT
        'Contradiction AI',
        CAST(@ContradictionScore AS VARCHAR(40)),
        CAST(CASE
            WHEN @ContradictionScore IS NOT NULL
             AND @ContradictionScore BETWEEN 1 AND @Contradiction_MaxScore THEN 1 ELSE 0 END AS BIT),
        CAST(CASE CAST(@ContradictionScore AS INT)
            WHEN 1 THEN 'P0' WHEN 2 THEN 'P1' WHEN 3 THEN 'P2' WHEN 4 THEN 'P3' WHEN 5 THEN 'P4'
            ELSE NULL END AS VARCHAR(4))

    UNION ALL
    -- AI Merge (#6 redo)
    SELECT
        'AI Merge',
        CASE
            WHEN @RedoBit = 1 THEN 'RedoInterview=1'
            WHEN UPPER(ISNULL(@Suspicious, '')) IN ('YES', 'Y', 'TRUE') THEN 'interviewer_suspicious=' + @Suspicious
            ELSE 'OK'
        END,
        CAST(CASE WHEN @RedoBit = 1 THEN 1 ELSE 0 END AS BIT),
        CAST(CASE WHEN @RedoBit = 1 THEN 'P0' ELSE NULL END AS VARCHAR(4));
END
GO

GO

PRINT '3/4 usp_GetTrialFlagsForReview...';
GO

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
            CAST(NULL AS DECIMAL(5,2)) AS ReportQualityScore,
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
            CAST(NULL AS VARCHAR(500)) AS SeverityBreakdown,
            CAST(NULL AS NVARCHAR(1000)) AS CandidateFeedbackComment
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
        rc.SeverityBreakdown,
        ISNULL(STUFF(CONCAT(
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.CandidateInteractionQuality)), '') IS NOT NULL THEN ' Â· ' + LTRIM(RTRIM(cfd.CandidateInteractionQuality)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.LimitedHandoncoding)), '') IS NOT NULL THEN ' Â· ' + LTRIM(RTRIM(cfd.LimitedHandoncoding)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.WeakInfrastructure)), '') IS NOT NULL THEN ' Â· ' + LTRIM(RTRIM(cfd.WeakInfrastructure)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.UnethicalPractices)), '') IS NOT NULL THEN ' Â· ' + LTRIM(RTRIM(cfd.UnethicalPractices)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.TimeManagement)), '') IS NOT NULL THEN ' Â· ' + LTRIM(RTRIM(cfd.TimeManagement)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.PoorCommunicationSkills)), '') IS NOT NULL THEN ' Â· ' + LTRIM(RTRIM(cfd.PoorCommunicationSkills)) ELSE '' END,
            CASE WHEN NULLIF(LTRIM(RTRIM(cfd.InterviewerVideo)), '') IS NOT NULL THEN ' Â· ' + LTRIM(RTRIM(cfd.InterviewerVideo)) ELSE '' END
        ), 1, 3, ''), '') AS CandidateFeedbackComment
    FROM dbo.TrialFlags tf
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
    LEFT JOIN dbo.InterviewComments ic ON ic.InterviewId = tf.TriggeringInterviewId
    OUTER APPLY (
        SELECT TOP 1 *
        FROM dbo.CandidateFeedbackDetails x
        WHERE x.InterviewId = tf.TriggeringInterviewId
        ORDER BY x.Id DESC
    ) cfd
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

GO

PRINT '4/4 usp_GetTrialJudgmentContext...';
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialJudgmentContext]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT
            CAST(0 AS BIT) AS IsEligible,
            CAST(0 AS BIT) AS IsFirstCompletedInterview,
            CAST(0 AS BIT) AS AlreadyFlagged,
            CAST(0 AS INT) AS ChannelId,
            CAST(0 AS INT) AS InterviewerId,
            CAST(0 AS INT) AS IrfId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST(NULL AS DECIMAL(5,2)) AS TranscriptQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS ReportQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS CandidateRating,
            CAST(NULL AS DECIMAL(8,2)) AS JoinTimeMinutes,
            CAST(NULL AS DECIMAL(8,2)) AS InterviewDurationMinutes,
            CAST(NULL AS INT) AS RequiredDurationMinutes
        WHERE 1 = 0;
        RETURN;
    END

    DECLARE @IrfName VARCHAR(50);
    DECLARE @InterviewerRaw VARCHAR(200);
    DECLARE @InterviewDate DATETIME;
    DECLARE @FirstInterviewId INT;
    DECLARE @ChannelId INT;
    DECLARE @InterviewerId INT;
    DECLARE @IrfId INT;

    SELECT
        @IrfName = im.IRFId,
        @InterviewerRaw = LTRIM(RTRIM(im.interviewer)),
        @InterviewDate = im.interviewDate
    FROM dbo.InterviewMeetings im
    WHERE im.id = @InterviewId;

    IF @IrfName IS NULL OR @InterviewerRaw IS NULL OR @InterviewerRaw = ''
    BEGIN
        SELECT
            CAST(0 AS BIT) AS IsEligible,
            CAST(0 AS BIT) AS IsFirstCompletedInterview,
            CAST(0 AS BIT) AS AlreadyFlagged,
            CAST(0 AS INT) AS ChannelId,
            CAST(0 AS INT) AS InterviewerId,
            CAST(0 AS INT) AS IrfId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST(NULL AS DECIMAL(5,2)) AS TranscriptQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS ReportQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS CandidateRating,
            CAST(NULL AS DECIMAL(8,2)) AS JoinTimeMinutes,
            CAST(NULL AS DECIMAL(8,2)) AS InterviewDurationMinutes,
            CAST(NULL AS INT) AS RequiredDurationMinutes;
        RETURN;
    END

    ;WITH CompletedOnIrf AS (
        SELECT
            im.id,
            im.interviewDate,
            ROW_NUMBER() OVER (ORDER BY im.interviewDate ASC, im.id ASC) AS rn
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.IRFId = @IrfName
          AND LTRIM(RTRIM(im.interviewer)) = @InterviewerRaw
          AND ISNULL(im.confirmationStatus, 0) = 1
          AND (
                ic.MeetingStatus IN (4, 6, 7, 8)
             OR ic.InterviewStatus IN (2, 3, 4, 5)
          )
    )
    SELECT @FirstInterviewId = id
    FROM CompletedOnIrf
    WHERE rn = 1;

    SELECT TOP 1
        @IrfId = ti.IRFId,
        @ChannelId = ti.id,
        @InterviewerId = ti.InterviewerId
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    CROSS APPLY (
        SELECT TOP 1 u.UserId
        FROM (
            SELECT ul.id AS UserId, 1 AS Priority
            FROM dbo.UserList ul
            WHERE LTRIM(RTRIM(ul.EmailId)) = @InterviewerRaw
            UNION ALL
            SELECT ul.id, 2
            FROM dbo.UserList ul
            WHERE ul.EmailId = im.interviewer
            UNION ALL
            SELECT ip.UserId, 3
            FROM dbo.InterviewerProfile ip
            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
            WHERE LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = @InterviewerRaw
        ) u
        ORDER BY u.Priority
    ) ru
    INNER JOIN dbo.TransInterviewer ti ON ti.IRFId = irf.id
       AND (
            ti.UserId = ru.UserId
            OR EXISTS (
                SELECT 1 FROM dbo.Interviewer iv
                WHERE iv.id = ti.InterviewerId AND iv.UserId = ru.UserId
            )
            OR EXISTS (
                SELECT 1 FROM dbo.InterviewerProfile ipr
                WHERE ipr.id = ti.InterviewerId AND ipr.UserId = ru.UserId
            )
       )
    WHERE im.id = @InterviewId
    ORDER BY ti.id;

    DECLARE @IsFirst BIT = CASE WHEN @FirstInterviewId = @InterviewId THEN 1 ELSE 0 END;
    DECLARE @AlreadyFlagged BIT = CASE
        WHEN @InterviewerId IS NOT NULL AND @IrfId IS NOT NULL
             AND EXISTS (SELECT 1 FROM dbo.TrialFlags tf WHERE tf.InterviewerId = @InterviewerId AND tf.IrfId = @IrfId)
        THEN 1 ELSE 0 END;

    DECLARE @JoinedOn DATETIME;
    DECLARE @InterviewInterviewer VARCHAR(200);

    SELECT @InterviewInterviewer = LTRIM(RTRIM(im.interviewer))
    FROM dbo.InterviewMeetings im
    WHERE im.id = @InterviewId;

    -- Prefer join rows matching interviewer email/nickname (cross-resolve both sides); else earliest join.
    SELECT TOP 1 @JoinedOn = j.Joinedon
    FROM dbo.InterviewMeetingsJoinTime j
    WHERE j.Interviewid = @InterviewId
      AND j.Joinedon IS NOT NULL
    ORDER BY
        CASE
            WHEN NULLIF(LTRIM(RTRIM(j.interviewer)), '') IS NOT NULL
             AND LTRIM(RTRIM(j.interviewer)) = @InterviewInterviewer THEN 0
            WHEN EXISTS (
                SELECT 1
                FROM dbo.UserList ul
                LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ul.id
                WHERE (
                        LTRIM(RTRIM(ul.EmailId)) = @InterviewInterviewer
                     OR LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = @InterviewInterviewer
                      )
                  AND (
                        LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(j.interviewer))
                     OR LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = LTRIM(RTRIM(j.interviewer))
                      )
            ) THEN 1
            ELSE 2
        END ASC,
        j.Joinedon ASC;

    DECLARE @ActualDuration INT;
    DECLARE @RequiredDuration INT;

    SELECT
        @ActualDuration = ic.VideoDuration,
        @RequiredDuration = pt.DurationSelectedMin
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    LEFT JOIN dbo.PlanTypeChargesMaster pt
        ON pt.CountryType = irf.JobCountry
       AND pt.PlanTypeEnumValue = irf.Heuristicsscore
       AND irf.CompanyType = pt.CompanyType
    WHERE im.id = @InterviewId;

    DECLARE @TranscriptScore DECIMAL(5,2);
    DECLARE @ReportScore DECIMAL(5,2);
    DECLARE @CandidateRating DECIMAL(5,2);

    SELECT
        @TranscriptScore = CAST(NULLIF(ic.AITranscriptAnalysisScore, 0) AS DECIMAL(5,2)),
        @ReportScore = CAST(NULLIF(ic.AIReportQCAnalysisScore, 0) AS DECIMAL(5,2))
    FROM dbo.InterviewComments ic
    WHERE ic.InterviewId = @InterviewId;

    SELECT TOP 1 @CandidateRating = CAST(cfd.Rating AS DECIMAL(5,2))
    FROM dbo.CandidateFeedbackDetails cfd
    WHERE cfd.Interviewid = @InterviewId
      AND cfd.Rating IS NOT NULL
      AND cfd.Rating > 0
    ORDER BY cfd.Id DESC;

    DECLARE @JoinMinutes DECIMAL(8,2) = CASE
        WHEN @JoinedOn IS NULL OR @InterviewDate IS NULL THEN NULL
        WHEN DATEDIFF(MINUTE, @InterviewDate, @JoinedOn) < 0 THEN 0
        ELSE CAST(DATEDIFF(MINUTE, @InterviewDate, @JoinedOn) AS DECIMAL(8,2))
    END;

    SELECT
        CAST(CASE WHEN @IsFirst = 1 AND @AlreadyFlagged = 0 AND @ChannelId IS NOT NULL AND @IrfId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsEligible,
        @IsFirst AS IsFirstCompletedInterview,
        @AlreadyFlagged AS AlreadyFlagged,
        ISNULL(@ChannelId, 0) AS ChannelId,
        ISNULL(@InterviewerId, 0) AS InterviewerId,
        ISNULL(@IrfId, 0) AS IrfId,
        @InterviewDate AS TriggeringInterviewDate,
        @TranscriptScore AS TranscriptQualityScore,
        @ReportScore AS ReportQualityScore,
        @CandidateRating AS CandidateRating,
        @JoinMinutes AS JoinTimeMinutes,
        CAST(NULLIF(@ActualDuration, 0) AS DECIMAL(8,2)) AS InterviewDurationMinutes,
        NULLIF(@RequiredDuration, 0) AS RequiredDurationMinutes;
END
GO

GO

PRINT 'Done — all 4 SPs deployed.';
GO

-- After SP deploy, run: HowTo_BackfillRiskCategory_Last90Days.sql
-- PR verification:
-- SELECT COUNT(*) AS JsonOnlyRedoCount FROM QcInterviewAIevaluation
-- WHERE ISNULL(AIMergeRedoInterview,0)=0
--   AND ISNULL(TRY_CAST(JSON_VALUE(MergedAIQCDetails,'$.RedoInterview') AS BIT),0)=1;
-- SELECT Severity, COUNT(*) AS Cnt FROM RiskCategory GROUP BY Severity ORDER BY Severity;

