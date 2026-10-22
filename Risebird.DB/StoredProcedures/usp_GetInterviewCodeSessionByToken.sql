IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewCodeSessionByToken'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewCodeSessionByToken
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- The candidate's code session page is not signed in: the token is the credential. This returns the
-- one session the token belongs to, with the interview's current date and planned duration so the
-- caller can expire the token with the interview (a rescheduled interview moves the expiry with it).
CREATE PROCEDURE [dbo].[usp_GetInterviewCodeSessionByToken]
    @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
           ics.Id, ics.InterviewId, ics.QuestionId, ics.QuestionText, ics.Token, ics.LanguageId,
           ics.SubmittedSource, ics.LastRunSource, ics.LastRunOutput, ics.LastRunStatus, ics.RunCount,
           ics.CodingTimeLimitInMinutes, ics.CreatedBy, ics.CreatedOn, ics.OpenedOn, ics.SubmittedOn,
           im.interviewDate AS InterviewDate,
           pt.DurationSelectedMin AS InterviewDurationMinutes,
           -- The rollout switch, from the interview's own channel: a session on a channel that has been
           -- switched off answers nothing, like every other alignment endpoint.
           CAST(ISNULL(irf.IsInterviewerAlignmentRequired, 0) AS BIT) AS IsInterviewerAlignmentRequired
    FROM dbo.InterviewCodeSessions ics
    INNER JOIN dbo.InterviewMeetings im ON im.id = ics.InterviewId
    LEFT JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    LEFT JOIN dbo.PlanTypeChargesMaster pt
        ON pt.CountryType = irf.JobCountry
       AND pt.PlanTypeEnumValue = irf.Heuristicsscore
       AND irf.CompanyType = pt.CompanyType
    WHERE ics.Token = @Token;
END
GO
