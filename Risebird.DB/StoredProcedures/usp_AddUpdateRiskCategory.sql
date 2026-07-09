IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateRiskCategory'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateRiskCategory
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =====================================================================
-- Procedure : usp_AddUpdateRiskCategory
-- Purpose   : Derives risk category data for the given InterviewId using
--             the same multi-source UNION logic as
--             usp_AutoApprovePaymentSlaDetails, then UPSERTs one row
--             per InterviewId into RiskCategory.
--             Name        = comma-separated severity labels
--                           (e.g. "High Severity,Under Review")
--             Description = comma-separated risk type reasons
--                           (e.g. "deficitmorethan10,CandidateRating")
-- Usage     : EXEC usp_AddUpdateRiskCategory @InterviewId = 1234
-- =====================================================================
CREATE PROCEDURE [dbo].[usp_AddUpdateRiskCategory]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- -------------------------------------------------------
    -- Step 1: Derive risk types and severity for @InterviewId
    --         using the same UNION sources as
    --         usp_AutoApprovePaymentSlaDetails
    -- -------------------------------------------------------
    DECLARE @Name        VARCHAR(MAX)
    DECLARE @Description VARCHAR(MAX)

    SELECT
        @Name        = STRING_AGG(x.Riskcategory, ','),
        @Description = STRING_AGG(x.Type, ',')
    FROM (

        -- Video duration deficit > 10 min (selected)
        SELECT im.id AS InterviewId,
               'deficitmorethan10'     AS Type,
               'High Severity'         AS Riskcategory
        FROM InterviewMeetings im
        JOIN InterviewComments ic     ON ic.InterviewId  = im.id
        JOIN IRF                      ON IRF.IRFId        = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType   = IRF.JobCountry
                                     AND pt.PlanTypeEnumValue = IRF.Heuristicsscore
                                     AND IRF.CompanyType  = pt.CompanyType
        WHERE im.id = @InterviewId
          AND ic.MeetingStatus = 4
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND (ic.VideoDuration - pt.DurationSelectedMin) > (-pt.DurationSelectedMin)
          AND (ic.VideoDuration - pt.DurationSelectedMin) < -10

        UNION

        -- AI transcript analysis score < 4
        SELECT im.id,
               'AIVerdictLessthan4'    AS Type,
               'High Severity'         AS Riskcategory
        FROM InterviewMeetings im
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF                  ON IRF.IRFId       = im.IRFId
        JOIN Client cl            ON cl.id           = IRF.ClientId
        WHERE im.id = @InterviewId
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND ic.AITranscriptAnalysisScore < 4

        UNION

        -- Candidate feedback rating < 4
        SELECT im.id,
               'CandidateRating'       AS Type,
               'High Severity'         AS Riskcategory
        FROM InterviewMeetings im
        JOIN InterviewComments ic        ON ic.InterviewId   = im.id
        JOIN IRF                         ON IRF.IRFId         = im.IRFId
        JOIN Client cl                   ON cl.id             = IRF.ClientId
        JOIN CandidateFeedbackDetails cfd ON cfd.InterviewId  = im.id
        WHERE im.id = @InterviewId
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND cfd.Rating < 4

        UNION

        -- Video duration < 2/3 of min for selected candidates
        SELECT im.id,
               'Selectdeficit'         AS Type,
               'Under Review'          AS Riskcategory
        FROM InterviewMeetings im
        JOIN InterviewComments ic     ON ic.InterviewId  = im.id
        JOIN IRF                      ON IRF.IRFId        = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType   = IRF.JobCountry
                                     AND pt.PlanTypeEnumValue = IRF.Heuristicsscore
                                     AND IRF.CompanyType  = pt.CompanyType
        WHERE im.id = @InterviewId
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND (pt.DurationSelectedMin * 2 / 3 > ic.VideoDuration)
          AND ic.InterviewStatus IN (2, 4)

        UNION

        -- Video duration < 4/9 of min for rejected candidates
        SELECT im.id,
               'Rejectdeficit'         AS Type,
               'Under Review'          AS Riskcategory
        FROM InterviewMeetings im
        JOIN InterviewComments ic     ON ic.InterviewId  = im.id
        JOIN IRF                      ON IRF.IRFId        = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType   = IRF.JobCountry
                                     AND pt.PlanTypeEnumValue = IRF.Heuristicsscore
                                     AND IRF.CompanyType  = pt.CompanyType
        WHERE im.id = @InterviewId
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND (pt.DurationSelectedMin * 4 / 9 > ic.VideoDuration)
          AND ic.InterviewStatus IN (1, 3, 5)

        UNION

        -- AI QC evaluation marked as redo
        SELECT qca.InterviewId,
               'AIRedo'                AS Type,
               'Redo Requested'        AS Riskcategory
        FROM QcInterviewAIevaluation qca
        WHERE qca.InterviewId = @InterviewId
          AND ISNULL(qca.AIMergeRedoInterview, 0) = 1

        UNION

        -- Report AI QC score < 60
        SELECT InterviewId,
               'Report AI Score'       AS Type,
               'High Severity'         AS Riskcategory
        FROM InterviewComments
        WHERE InterviewId = @InterviewId
          AND AIReportQCAnalysisScore < 60

        UNION

        -- Transcript AI score < 6
        SELECT InterviewId,
               'Transcript AI Score'   AS Type,
               'High Severity'         AS Riskcategory
        FROM InterviewComments
        WHERE InterviewId = @InterviewId
          AND AITranscriptAnalysisScore < 6

        UNION

        -- AI contradiction score >= 4
        SELECT qca.InterviewId,
               'Contradiction AI Score' AS Type,
               'High Severity'          AS Riskcategory
        FROM QcInterviewAIevaluation qca
        WHERE qca.InterviewId = @InterviewId
          AND ISNULL(TRY_CAST(qca.ContradictionScore AS DECIMAL(10,2)), 0) >= 4

        UNION

        -- No show / interviewer no show / candidate no show
        SELECT im.id,
               CASE
                   WHEN imj.Joinedon    IS NULL AND imc.PerformedOn IS NULL THEN 'No Show'
                   WHEN imj.Joinedon    IS NULL                             THEN 'Interviewer No Show'
                   WHEN imc.PerformedOn IS NULL                             THEN 'Candidate No Show'
               END                     AS Type,
               'Fake Report'           AS Riskcategory
        FROM InterviewMeetings im
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        OUTER APPLY (
            SELECT MAX(Joinedon) AS Joinedon
            FROM InterviewMeetingsJoinTime
            WHERE Interviewid = im.id
        ) imj
        OUTER APPLY (
            SELECT MAX(PerformedOn) AS PerformedOn
            FROM PageClickDetails
            WHERE ButtonAction = 'JoinMeeting'
              AND Interviewid   = im.id
              AND ProfileType   = 'Candidate'
        ) imc
        WHERE im.id = @InterviewId
          AND DATEDIFF(DAY, im.interviewDate, GETDATE()) < 30
          AND im.confirmationStatus = 1
          AND im.ZoomMeetingId IN (SELECT DISTINCT Meetingid FROM ZoomMeetingTransmission)
          AND (imj.Joinedon IS NULL OR imc.PerformedOn IS NULL)
          AND ic.MeetingStatus NOT IN (1, 2, 3, 5, 9)

    ) x
    WHERE x.InterviewId = @InterviewId

    -- -------------------------------------------------------
    -- Step 2: UPSERT — one row per InterviewId
    --         UPDATE always (if row exists).
    --         INSERT only when Description has a value.
    -- -------------------------------------------------------
    IF EXISTS (SELECT 1 FROM RiskCategory WHERE InterviewId = @InterviewId)
    BEGIN
        UPDATE RiskCategory
        SET
            Name        = @Name,
            Description = @Description,
            ModifiedOn  = GETDATE()
        WHERE InterviewId = @InterviewId
    END
    ELSE IF ISNULL(LTRIM(RTRIM(@Description)), '') <> ''
    BEGIN
        INSERT INTO RiskCategory (InterviewId, Name, Description, CreatedOn)
        VALUES (@InterviewId, @Name, @Description, GETDATE())
    END

END
GO
