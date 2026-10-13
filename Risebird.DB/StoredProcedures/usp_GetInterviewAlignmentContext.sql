IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewAlignmentContext'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewAlignmentContext
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Everything the interview alignment page (Interview/InterviewDetails) needs beyond what
-- usp_FetchAudioComment already binds: role and duration for the candidate card, whether the channel
-- runs trials, whether this interview is the interviewer's trial, and whether they have already been
-- through the one-time trial onboarding.
--
-- "Is this a trial" is the pre-interview mirror of usp_GetTrialJudgmentContext.IsFirstCompletedInterview:
-- the interview is a trial when no completed interview by the same interviewer on the same channel
-- comes before it. Both read fn_InterviewerCompletedInterviewsOnIrf, so they cannot drift apart.
-- The banner only shows when IRF.AITrialEnabled = 1; NULL (every existing row) means off.
CREATE PROCEDURE [dbo].[usp_GetInterviewAlignmentContext]
    @InterviewId      INT,
    @InterviewerEmail VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IrfName        VARCHAR(50);
    DECLARE @InterviewerRaw VARCHAR(200);
    DECLARE @InterviewDate  DATETIME;

    SELECT
        @IrfName = im.IRFId,
        @InterviewerRaw = LTRIM(RTRIM(im.interviewer)),
        @InterviewDate = im.interviewDate
    FROM dbo.InterviewMeetings im
    WHERE im.id = @InterviewId;

    DECLARE @CompletedBefore INT = 0;
    IF @IrfName IS NOT NULL AND ISNULL(@InterviewerRaw, '') <> ''
    BEGIN
        SELECT @CompletedBefore = COUNT(DISTINCT c.id)
        FROM dbo.fn_InterviewerCompletedInterviewsOnIrf(@IrfName, @InterviewerRaw) c
        WHERE c.id <> @InterviewId
          AND (c.interviewDate < @InterviewDate
               OR (c.interviewDate = @InterviewDate AND c.id < @InterviewId));
    END

    DECLARE @InterviewerUserId INT;
    DECLARE @OnboardingAcknowledged BIT;
    DECLARE @OnboardingAcknowledgedOn DATETIME;
    DECLARE @OnboardingPolicyVersion VARCHAR(50);

    SELECT TOP 1
        @InterviewerUserId = ul.id,
        @OnboardingAcknowledged = ip.TrialOnboardingAcknowledged,
        @OnboardingAcknowledgedOn = ip.TrialOnboardingAcknowledgedOn,
        @OnboardingPolicyVersion = ip.TrialOnboardingPolicyVersion
    FROM dbo.UserList ul
    LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ul.id
    WHERE LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(@InterviewerEmail))
    ORDER BY ip.Id DESC;

    SELECT TOP 1
        irf.id                                                    AS IRFIntId,
        im.IRFId                                                  AS IRFCode,
        irf.JDTiltle                                              AS RoleTitle,
        pt.DurationSelectedMin                                    AS DurationMinutes,
        CAST(ISNULL(irf.CodeSnippetRequired, 0) AS BIT)           AS CodeSnippetRequired,
        CAST(ISNULL(irf.AITrialEnabled, 0) AS BIT)                AS AITrialEnabled,
        CAST(CASE WHEN ISNULL(irf.AITrialEnabled, 0) = 1 AND @CompletedBefore = 0
                  THEN 1 ELSE 0 END AS BIT)                       AS IsTrialInterview,
        @CompletedBefore                                          AS CompletedOnChannelBefore,
        @InterviewerUserId                                        AS InterviewerUserId,
        CAST(ISNULL(@OnboardingAcknowledged, 0) AS BIT)           AS TrialOnboardingAcknowledged,
        @OnboardingAcknowledgedOn                                 AS TrialOnboardingAcknowledgedOn,
        @OnboardingPolicyVersion                                  AS TrialOnboardingPolicyVersion,
        -- Only used server-side to look up the candidate's CandidatePseudonym; never rendered.
        LTRIM(RTRIM(im.candidateEmail))                           AS CandidateEmail
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    LEFT JOIN dbo.PlanTypeChargesMaster pt
        ON pt.CountryType = irf.JobCountry
       AND pt.PlanTypeEnumValue = irf.Heuristicsscore
       AND irf.CompanyType = pt.CompanyType
    WHERE im.id = @InterviewId;
END
GO
