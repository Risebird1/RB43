IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateRiskCategory'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateRiskCategory
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddUpdateRiskCategory]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DeficitMoreThan10_Min INT = 10
    DECLARE @Deficit_P2_Max INT = 20
    DECLARE @Deficit_P1_Max INT = 30
    DECLARE @TranscriptLt4_P2_Min INT = 3
    DECLARE @TranscriptLt4_P1_Min INT = 2
    DECLARE @RatingLt4_P2_Min INT = 3
    DECLARE @RatingLt4_P1_Min INT = 2
    DECLARE @SelectDeficit_Ratio_P3_Min DECIMAL(10,4) = 0.5
    DECLARE @SelectDeficit_Ratio_P2_Min DECIMAL(10,4) = 0.333333
    DECLARE @SelectDeficit_Ratio_P1_Max DECIMAL(10,4) = 0.333333
    DECLARE @RejectDeficit_Ratio_P4_Min DECIMAL(10,4) = 0.333333
    DECLARE @RejectDeficit_Ratio_P3_Min DECIMAL(10,4) = 0.222222
    DECLARE @RejectDeficit_Ratio_P2_Max DECIMAL(10,4) = 0.222222
    DECLARE @ReportScoreLt60_P2_Max INT = 60
    DECLARE @ReportScoreLt60_P1_Max INT = 40
    DECLARE @ReportScoreLt60_P0_Max INT = 20
    DECLARE @TranscriptLt6_P4_Min INT = 5
    DECLARE @TranscriptLt6_P3_Min INT = 4
    DECLARE @TranscriptLt6_P2_Min INT = 3
    DECLARE @TranscriptLt6_P1_Min INT = 2
    DECLARE @Contradiction_MaxScore INT = 5

    DECLARE @Name               VARCHAR(MAX)
    DECLARE @Description        VARCHAR(MAX)
    DECLARE @Severity           VARCHAR(4)
    DECLARE @SeverityBreakdown  VARCHAR(500)
    DECLARE @MinPLevel          INT

    SELECT
        @Name              = STRING_AGG(x.Riskcategory, ','),
        @Description       = STRING_AGG(x.Type, ','),
        @SeverityBreakdown = STRING_AGG(x.Type + ':P' + CAST(x.PLevel AS VARCHAR(1)), ','),
        @MinPLevel         = MIN(x.PLevel)
    FROM (
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
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND (ic.VideoDuration - pt.DurationSelectedMin) > (-pt.DurationSelectedMin)
          AND (ic.VideoDuration - pt.DurationSelectedMin) < -@DeficitMoreThan10_Min
        UNION ALL
        -- #2 AIVerdictLessthan4
        SELECT im.id, 'AIVerdictLessthan4', 'High Severity',
               CASE WHEN NULLIF(ic.AITranscriptAnalysisScore,0) < @TranscriptLt4_P1_Min THEN 0
                    WHEN NULLIF(ic.AITranscriptAnalysisScore,0) < @TranscriptLt4_P2_Min THEN 1 ELSE 2 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId JOIN Client cl ON cl.id = IRF.ClientId
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND NULLIF(ic.AITranscriptAnalysisScore,0) < 4
        UNION ALL
        -- #3 CandidateRating
        SELECT im.id, 'CandidateRating', 'High Severity',
               CASE WHEN cfd.Rating < @RatingLt4_P1_Min THEN 0
                    WHEN cfd.Rating < @RatingLt4_P2_Min THEN 1 ELSE 2 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId JOIN Client cl ON cl.id = IRF.ClientId
        JOIN CandidateFeedbackDetails cfd ON cfd.InterviewId = im.id
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30 AND cfd.Rating < 4
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
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30 AND pt.DurationSelectedMin > 0
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
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30 AND pt.DurationSelectedMin > 0
          AND (pt.DurationSelectedMin * 4 / 9 > ic.VideoDuration) AND ic.InterviewStatus IN (1,3,5)
        UNION ALL
        -- #6 AIRedo
        SELECT qca.InterviewId, 'AIRedo', 'Redo Requested', 0
        FROM QcInterviewAIevaluation qca
        WHERE qca.InterviewId = @InterviewId AND ISNULL(qca.AIMergeRedoInterview,0) = 1
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
        WHERE im.id = @InterviewId AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND im.confirmationStatus = 1
          AND im.ZoomMeetingId IN (SELECT DISTINCT Meetingid FROM ZoomMeetingTransmission)
          AND (imj.Joinedon IS NULL OR imc.PerformedOn IS NULL)
          AND ic.MeetingStatus NOT IN (1,2,3,5,9)
    ) x
    WHERE x.InterviewId = @InterviewId AND x.PLevel IS NOT NULL

    IF @MinPLevel IS NOT NULL
        SET @Severity = 'P' + CAST(@MinPLevel AS VARCHAR(1))

    IF EXISTS (SELECT 1 FROM RiskCategory WHERE InterviewId = @InterviewId)
        UPDATE RiskCategory SET Name=@Name, Description=@Description,
               Severity=@Severity, SeverityBreakdown=@SeverityBreakdown, ModifiedOn=GETDATE()
        WHERE InterviewId = @InterviewId
    ELSE IF ISNULL(LTRIM(RTRIM(@Description)), '') <> ''
        INSERT INTO RiskCategory (InterviewId, Name, Description, Severity, SeverityBreakdown, CreatedOn)
        VALUES (@InterviewId, @Name, @Description, @Severity, @SeverityBreakdown, GETDATE())
END
GO