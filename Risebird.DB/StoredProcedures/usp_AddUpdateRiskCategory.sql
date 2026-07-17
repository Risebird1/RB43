CREATE OR ALTER PROCEDURE [dbo].[usp_AddUpdateRiskCategory]
    @InterviewId INT
AS
/*
  Invariant: a RiskCategory row exists IFF at least one check currently fires for @InterviewId.
  Severity and Description are NEVER NULL on a surviving row.
  When zero checks fire: DELETE any existing row (do not leave a husk with NULL fields).
*/
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
    DECLARE @LateJoin_P1_Min INT = 10;  -- j > 10 → P1; 5 < j <= 10 → P2
    -- #14–16 PQI Category thresholds (aligned with skill-channel / reliability scores)
    DECLARE @LessQuestions_Max INT = 5;           -- trigger when QuestionsDiscussed count < 5
    DECLARE @LessSelectComment_MaxLen INT = 500;  -- select short comment
    DECLARE @LessRejectComment_MaxLen INT = 250;  -- reject short comment
    DECLARE @PQI_PLevel INT = 2;                  -- PQI Category → P2

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
        -- #6 AIRedo — dual source: bit column OR MergedAIQCDetails JSON (same pattern as IRF audit directory)
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
        -- #11 InterviewerVideoOff → P0 only on explicit "Video Off" text (never NULL/empty/'No'/VideoEnable)
        SELECT im.id, 'InterviewerVideoOff', 'Fake Report', 0
        FROM InterviewMeetings im
        WHERE im.id = @InterviewId
          AND EXISTS (
                SELECT 1
                FROM PaymentSLADetails p
                WHERE p.Interviewid = im.id
                  AND p.InterviewerVideo IS NOT NULL
                  AND LTRIM(RTRIM(p.InterviewerVideo)) <> ''
                  AND UPPER(LTRIM(RTRIM(p.InterviewerVideo))) NOT IN ('NO', 'N/A', 'NA', 'NONE')
                  AND p.InterviewerVideo LIKE '%Video Off%'
              )

        UNION ALL
        -- #12 CandidateRedoRequest → P0 only when a non-rejected RedoInterviews row exists
        SELECT im.id, 'CandidateRedoRequest', 'Redo Requested', 0
        FROM InterviewMeetings im
        WHERE im.id = @InterviewId
          AND EXISTS (
                SELECT 1 FROM RedoInterviews ri
                WHERE ri.interviewid = im.id
                  AND ISNULL(ri.Rejected, 0) = 0
              )

        UNION ALL
        -- #13 InterviewerLateJoin — graded; NULL/unmatched join time = not triggered (CROSS APPLY must match)
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

        UNION ALL
        -- #14 LessQuestions (PQI) — fewer than 5 questions discussed (includes 0)
        SELECT im.id, 'LessQuestions', 'PQI Category', @PQI_PLevel
        FROM InterviewMeetings im
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.id = @InterviewId
          AND im.confirmationStatus = 1
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND (
                SELECT COUNT(qd.id)
                FROM QuestionsDiscussed qd
                WHERE qd.InterviewId = im.id
              ) < @LessQuestions_Max

        UNION ALL
        -- #15 LessSelectComment (PQI) — select with short interviewer comment
        SELECT im.id, 'LessSelectComment', 'PQI Category', @PQI_PLevel
        FROM InterviewMeetings im
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.id = @InterviewId
          AND im.confirmationStatus = 1
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND ic.InterviewStatus IN (2, 4)
          AND LEN(ISNULL(ic.InterviewerComment, '')) < @LessSelectComment_MaxLen

        UNION ALL
        -- #16 LessRejectComment (PQI) — reject with short interviewer comment
        SELECT im.id, 'LessRejectComment', 'PQI Category', @PQI_PLevel
        FROM InterviewMeetings im
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.id = @InterviewId
          AND im.confirmationStatus = 1
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND ic.InterviewStatus IN (3, 5)
          AND LEN(ISNULL(ic.InterviewerComment, '')) < @LessRejectComment_MaxLen
    )
    SELECT
        @Name              = STRING_AGG(x.Riskcategory, ','),
        @Description       = STRING_AGG(x.Type, ','),
        @SeverityBreakdown = STRING_AGG(x.Type + ':P' + CAST(x.PLevel AS VARCHAR(1)), ','),
        @MinPLevel         = MIN(x.PLevel)
    FROM Checks x
    WHERE x.InterviewId = @InterviewId
      AND x.PLevel IS NOT NULL;

    -- Overall severity = MIN(PLevel); P0 = highest severity … P5 = clean
    IF @MinPLevel IS NOT NULL
        SET @Severity = 'P' + CAST(@MinPLevel AS VARCHAR(1));

    -- Step 2 upsert: row exists only when at least one check fired (Description non-empty, Severity set).
    IF ISNULL(LTRIM(RTRIM(@Description)), '') = '' OR @Severity IS NULL
    BEGIN
        -- Zero checks (or incomplete aggregate): DELETE husk if present; otherwise no-op.
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
