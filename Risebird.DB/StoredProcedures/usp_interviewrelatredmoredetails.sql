IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_interviewrelatredmoredetails'))
BEGIN
    DROP PROCEDURE dbo.usp_interviewrelatredmoredetails
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_interviewrelatredmoredetails
    @Interviewid INT
AS
BEGIN
    SELECT
        ic.InterviewStatus,
        ic.MeetingStatus,
        pm.DurationSelectedMin                                              AS interviewDuration,
        (pm.DurationSelectedMin - ic.VideoDuration)                        AS deficit,
        CASE
            WHEN DATEDIFF(MINUTE, im.interviewDate, imj.Joinedon) >= 5
            THEN 1 ELSE 0
        END                                                                AS InterviewerLateJoin,
        cfd.Rating,
        imj.Joinedon,
        IRF.IsVideoRequired,

        -- Interviewer video status reported by the candidate
        cfd.InterviewerVideo,

        -- Whether the candidate raised a redo request (entry in RedoInterviews by candidate)
        CASE WHEN ri.id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS RedoInterview,

        -- Combined candidate comment from all complaint columns (non-null / non-empty values joined with ', ')
        NULLIF(
            STUFF(
                ISNULL(', ' + NULLIF(LTRIM(RTRIM(cfd.CandidateInteractionQuality)),  ''), '') +
                ISNULL(', ' + NULLIF(LTRIM(RTRIM(cfd.LimitedHandoncoding)),           ''), '') +
                ISNULL(', ' + NULLIF(LTRIM(RTRIM(cfd.WeakInfrastructure)),            ''), '') +
                ISNULL(', ' + NULLIF(LTRIM(RTRIM(cfd.UnethicalPractices)),            ''), '') +
                ISNULL(', ' + NULLIF(LTRIM(RTRIM(cfd.TimeManagement)),                ''), '') +
                ISNULL(', ' + NULLIF(LTRIM(RTRIM(cfd.PoorCommunicationSkills)),       ''), '')
            , 1, 2, '')
        , '') AS CandidateComment

    FROM InterviewMeetings im
    LEFT JOIN InterviewComments          ic  ON ic.Interviewid        = im.id
    LEFT JOIN IRF                            ON IRF.IRFId              = im.IRFId
    LEFT JOIN PlanTypeChargesMaster      pm  ON pm.CountryType         = IRF.JobCountry
                                            AND pm.CompanyType         = IRF.CompanyType
                                            AND pm.PlanTypeEnumValue   = IRF.Heuristicsscore
    LEFT JOIN CandidateFeedbackDetails   cfd ON cfd.Interviewid        = im.id
    LEFT JOIN RedoInterviews             ri  ON ri.interviewid         = im.id
                                            AND ri.CreatedBy           = im.candidateEmail
                                            AND ri.Type                = 'Candidate'
    OUTER APPLY (
        SELECT MIN(Joinedon) AS Joinedon
        FROM   InterviewMeetingsJoinTime
        WHERE  interviewid = im.id
    ) AS imj
    WHERE im.id = @Interviewid
END
GO
