-- =====================================================================================================
-- Interview alignment + per-question code sessions (RB38 handoff) - COMPLETE DEPLOYMENT SCRIPT
-- Run this one file on the database (SSMS / sqlcmd). Every part is idempotent: safe to run again.
-- Confirm which branch deploys (dev2026 vs master) before running it against a shared database.
--
-- Contents, in the order they must run (columns before the procedures that read them):
--    1. Schema: 7 columns (IRF x2, InterviewerProfile x3, TechSkillComments x2) + 2 new tables
--    2. fn_InterviewerCompletedInterviewsOnIrf        NEW function (shared trial predicate)
--    3. usp_GetTrialJudgmentContext                   CHANGED (reads the shared function; same results)
--    4. usp_FetchAudioComment                         CHANGED (also returns IRF.IsInterviewerAlignmentRequired)
--    5. usp_SetInterviewerAlignmentRequired           NEW (the Edit IRF switch; writes only that column)
--    6. usp_GetInterviewAlignmentRequired             NEW (the rollout switch for one interview)
--    7. usp_GetInterviewAlignmentContext              NEW
--    8. usp_GetInterviewAlignmentFlaggedInterviews    NEW (read-only, "What went wrong before")
--    9. usp_AcknowledgeTrialOnboarding                NEW
--   10. usp_SaveJoinGateOverride                      NEW
--   11. usp_GetInterviewerCodeQuestions               NEW
--   12. usp_CreateInterviewCodeSession                NEW
--   13. usp_GetInterviewCodeSessions                  NEW
--   14. usp_GetInterviewCodeSessionByToken            NEW
--   15. usp_MarkInterviewCodeSessionOpened            NEW
--   16. usp_SaveInterviewCodeSessionRun               NEW
--   17. usp_SubmitInterviewCodeSession                NEW
--   18. usp_AddTechSkillComments                      CHANGED (keeps the coding flag across saves)
--
-- Generated from the individual files in Risebird.DB; those remain the source of truth.
-- =====================================================================================================
GO

-- -----------------------------------------------------------------------------------------------------
-- 1. Migrations/InterviewAlignment_CodeSessions_Schema.sql
-- -----------------------------------------------------------------------------------------------------
-- Interview alignment + per-question code sessions (RB38 handoff) - schema.
-- Idempotent: every change is guarded, so running this twice is a no-op.
--
-- Adds:
--   IRF.IsInterviewerAlignmentRequired             bit NULL   (the rollout switch; NULL = today's page on every channel)
--   IRF.AITrialEnabled                             bit NULL   (NULL = trial banner off: every existing row)
--   InterviewerProfile.TrialOnboardingAcknowledged bit NULL
--   InterviewerProfile.TrialOnboardingAcknowledgedOn datetime NULL
--   InterviewerProfile.TrialOnboardingPolicyVersion varchar(50) NULL
--   TechSkillComments.IsCodingQuestion             bit NULL
--   TechSkillComments.CodingTimeLimitInMinutes     int NULL
--   InterviewCodeSessions                          (new table)
--   InterviewJoinGateOverrides                     (new table)
-- SkillChannelSampleQuestions is NOT touched: it already has IsCodingQuestion / CodingTimeLimitInMinutes
-- for the AI interview, and that flow is unchanged.
--
-- Deployment order (columns must exist before the procedures that read them are created):
--   1. this script (or Tables/IRF.sql, Tables/InterviewerProfile.sql, Tables/TechSkillComments.sql,
--      Tables/InterviewCodeSessions.sql, Tables/InterviewJoinGateOverrides.sql)
--   2. Functions/fn_InterviewerCompletedInterviewsOnIrf.sql
--   3. StoredProcedures/usp_GetTrialJudgmentContext.sql      (now reads the shared function)
--   4. StoredProcedures/usp_FetchAudioComment.sql             (now also returns IsInterviewerAlignmentRequired)
--      StoredProcedures/usp_SetInterviewerAlignmentRequired.sql (the Edit IRF switch; writes only that column)
--      StoredProcedures/usp_GetInterviewAlignmentRequired.sql
--      StoredProcedures/usp_GetInterviewAlignmentContext.sql
--      StoredProcedures/usp_GetInterviewAlignmentFlaggedInterviews.sql  (read-only: "What went wrong before")
--      StoredProcedures/usp_AcknowledgeTrialOnboarding.sql
--      StoredProcedures/usp_SaveJoinGateOverride.sql
--      StoredProcedures/usp_GetInterviewerCodeQuestions.sql
--      StoredProcedures/usp_CreateInterviewCodeSession.sql
--      StoredProcedures/usp_GetInterviewCodeSessions.sql
--      StoredProcedures/usp_GetInterviewCodeSessionByToken.sql
--      StoredProcedures/usp_MarkInterviewCodeSessionOpened.sql
--      StoredProcedures/usp_SaveInterviewCodeSessionRun.sql
--      StoredProcedures/usp_SubmitInterviewCodeSession.sql
--   5. StoredProcedures/usp_AddTechSkillComments.sql         (now keeps the coding flag across saves)
-- The web code degrades to today's page if any of these objects are missing, so it can ship first.
-- Confirm which branch deploys (dev2026 vs master) before running this against a shared database.

-- Part one --------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsInterviewerAlignmentRequired' AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF ADD IsInterviewerAlignmentRequired bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AITrialEnabled' AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF ADD AITrialEnabled bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingAcknowledged' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile ADD TrialOnboardingAcknowledged bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingAcknowledgedOn' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile ADD TrialOnboardingAcknowledgedOn datetime NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingPolicyVersion' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile ADD TrialOnboardingPolicyVersion varchar(50) NULL
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewJoinGateOverrides')
BEGIN
    CREATE TABLE [dbo].[InterviewJoinGateOverrides] (
        [Id]                      int IDENTITY(1,1) NOT NULL,
        [InterviewId]             int NOT NULL,
        [InterviewerEmail]        varchar(200) NOT NULL,
        [OutstandingRequirements] varchar(500) NULL,
        [CreatedOn]               datetime NOT NULL CONSTRAINT [DF_IJGO_CreatedOn] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_InterviewJoinGateOverrides] PRIMARY KEY CLUSTERED ([Id] ASC)
    )

    CREATE NONCLUSTERED INDEX [IX_IJGO_InterviewId] ON [dbo].[InterviewJoinGateOverrides]([InterviewId])
END
GO

-- Part two --------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsCodingQuestion' AND Object_ID = Object_ID(N'TechSkillComments'))
BEGIN
    ALTER TABLE TechSkillComments ADD IsCodingQuestion bit NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CodingTimeLimitInMinutes' AND Object_ID = Object_ID(N'TechSkillComments'))
BEGIN
    ALTER TABLE TechSkillComments ADD CodingTimeLimitInMinutes int NULL
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewCodeSessions')
BEGIN
    CREATE TABLE [dbo].[InterviewCodeSessions] (
        [Id]                       int IDENTITY(1,1) NOT NULL,
        [InterviewId]              int NOT NULL,
        [QuestionId]               int NOT NULL,
        [QuestionText]             nvarchar(max) NOT NULL,
        [Token]                    uniqueidentifier NOT NULL,
        [LanguageId]               varchar(40) NULL,
        [SubmittedSource]          nvarchar(max) NULL,
        [LastRunSource]            nvarchar(max) NULL,
        [LastRunOutput]            nvarchar(max) NULL,
        [LastRunStatus]            varchar(40) NULL,
        [RunCount]                 int NOT NULL CONSTRAINT [DF_ICS_RunCount] DEFAULT (0),
        [CodingTimeLimitInMinutes] int NULL,
        [CreatedBy]                varchar(200) NULL,
        [CreatedOn]                datetime NOT NULL CONSTRAINT [DF_ICS_CreatedOn] DEFAULT (GETUTCDATE()),
        [OpenedOn]                 datetime NULL,
        [SubmittedOn]              datetime NULL,
        CONSTRAINT [PK_InterviewCodeSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_ICS_Token] UNIQUE ([Token]),
        CONSTRAINT [UQ_ICS_InterviewQuestion] UNIQUE ([InterviewId], [QuestionId])
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_ICS_InterviewId' AND object_id = OBJECT_ID(N'dbo.InterviewCodeSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ICS_InterviewId] ON [dbo].[InterviewCodeSessions]([InterviewId])
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 2. Functions/fn_InterviewerCompletedInterviewsOnIrf.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_InterviewerCompletedInterviewsOnIrf]')
                      AND type IN (N'IF', N'TF', N'FN'))
BEGIN
    DROP FUNCTION dbo.fn_InterviewerCompletedInterviewsOnIrf
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Single source of truth for "which interviews has this interviewer completed on this channel".
--
-- The trial subsystem defines a trial interview as the interviewer's first completed interview on a
-- channel. Two readers need that predicate and must never disagree:
--   * usp_GetTrialJudgmentContext      - after the interview, picks the first completed one (rn = 1)
--   * usp_GetInterviewAlignmentContext - before the interview, asks whether any completed one
--                                        precedes this one, to decide whether to show the trial banner
-- Both read this function, so the banner cannot say "trial" while the flag job says otherwise.
--
-- @IrfName is InterviewMeetings.IRFId (the channel code, not IRF.id). @Interviewer is matched the way
-- the trial job always has: trimmed, exact.
CREATE FUNCTION [dbo].[fn_InterviewerCompletedInterviewsOnIrf]
(
    @IrfName     varchar(50),
    @Interviewer varchar(200)
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        im.id,
        im.interviewDate
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.IRFId = @IrfName
      AND LTRIM(RTRIM(im.interviewer)) = @Interviewer
      AND ISNULL(im.confirmationStatus, 0) = 1
      AND (
            ic.MeetingStatus IN (4, 6, 7, 8)
         OR ic.InterviewStatus IN (2, 3, 4, 5)
      )
)
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 3. StoredProcedures/usp_GetTrialJudgmentContext.sql
-- -----------------------------------------------------------------------------------------------------
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

    -- "Completed on this channel" lives in fn_InterviewerCompletedInterviewsOnIrf so the pre-interview
    -- trial banner (usp_GetInterviewAlignmentContext) reads exactly the same predicate.
    ;WITH CompletedOnIrf AS (
        SELECT
            c.id,
            c.interviewDate,
            ROW_NUMBER() OVER (ORDER BY c.interviewDate ASC, c.id ASC) AS rn
        FROM dbo.fn_InterviewerCompletedInterviewsOnIrf(@IrfName, @InterviewerRaw) c
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

-- -----------------------------------------------------------------------------------------------------
-- 4. StoredProcedures/usp_FetchAudioComment.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchAudioComment'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchAudioComment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchAudioComment
@interviewId int
as 
begin
Select irf.AudioComment,irf.CompanyDetails,im.zoomLink,im.interviewDate,irf.JDLink,irf.JobLevel,im.candidateEmail,im.MobileNumber,im.id,im.candidateName,irf.IRFID,cf.FormURL as ConsentFormUrl,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,IRF.Heuristicsscore,IRF.IsVideoRequired,cl.IsCandidateVerification,
IRF.CandidateIdverification,IRF.CodeSnippetRequired,IRF.CandidateVideo,
-- Interview alignment rollout switch, read in the same round trip as the rest of Interview Details.
IRF.IsInterviewerAlignmentRequired
from InterviewMeetings im                   
 join IRF  irf on im.IRFId=irf.IRFId Left join ConsentForms cf on cf.IRFId=IRF.id
 join Client cl on cl.id=IRF.ClientId
 join UserList ul on ul.EmailId=im.candidateEmail
 join CandidatesMaster cm on cm.Userid=ul.id
 join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id
 where im.id=@interviewId
 End
GO
GO

-- -----------------------------------------------------------------------------------------------------
-- 5. StoredProcedures/usp_SetInterviewerAlignmentRequired.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SetInterviewerAlignmentRequired'))
BEGIN
    DROP PROCEDURE dbo.usp_SetInterviewerAlignmentRequired
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Switches the interview alignment page on or off for one channel (IRF.IsInterviewerAlignmentRequired),
-- from the "Interviewer alignment required" checkbox on Edit IRF.
--
-- This is deliberately its own write, touching this one column and nothing else. The switch used to be
-- saved through usp_UpdateIRFDetails with the rest of the Edit IRF form, which re-applies the channel's
-- interviewer panel on every save (MAG membership forces IsActive = 0, and every future interview of an
-- inactive interviewer is unassigned). Toggling the switch must never change who is active on the
-- channel or which interviews are assigned, so it no longer goes anywhere near that save.
CREATE PROCEDURE [dbo].[usp_SetInterviewerAlignmentRequired]
    @IrfId      INT,
    @IsRequired BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.IRF
       SET IsInterviewerAlignmentRequired = @IsRequired
     WHERE id = @IrfId;

    SELECT @@ROWCOUNT AS UpdatedRows;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 6. StoredProcedures/usp_GetInterviewAlignmentRequired.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewAlignmentRequired'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewAlignmentRequired
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- The interview alignment rollout switch for one interview, read from that interview's own channel.
--
-- Every alignment endpoint (trial acknowledgement, join-gate override, code-session questions and
-- minting, the inline insights, and the candidate's code-session page) calls this alongside
-- IsInterviewerForInterview. A page gate does not protect a route, and the flag must never come from
-- anything the caller supplies - so it is resolved here from InterviewMeetings -> IRF, nowhere else.
-- NULL (every channel on day one) and 0 both mean off.
CREATE PROCEDURE [dbo].[usp_GetInterviewAlignmentRequired]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(ISNULL(MAX(CAST(irf.IsInterviewerAlignmentRequired AS INT)), 0) AS BIT) AS IsInterviewerAlignmentRequired
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.id = @InterviewId;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 7. StoredProcedures/usp_GetInterviewAlignmentContext.sql
-- -----------------------------------------------------------------------------------------------------
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
        CAST(ISNULL(irf.IsInterviewerAlignmentRequired, 0) AS BIT) AS IsInterviewerAlignmentRequired,
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

GO

-- -----------------------------------------------------------------------------------------------------
-- 8. StoredProcedures/usp_GetInterviewAlignmentFlaggedInterviews.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewAlignmentFlaggedInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewAlignmentFlaggedInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- "What went wrong before" on the interview alignment page: the signed-in interviewer's own past interviews on
-- this channel where any quality signal fired. Ported from the flagged-interviews part of the earlier
-- usp_GetInterviewAlignmentDetails (branch dm-modify); nothing else of that procedure is used.
--
-- Read-only. It writes nothing and changes no existing object. It returns rows only when @InterviewerEmail is
-- the interviewer of @InterviewId, and only that interviewer's own interviews.
--
-- Signals (the existing data conventions):
--   IsSuspiciousQC                 - latest QC form found a suspicious app or red flags
--   IsRedo                         - the interview was redone, or a redo was approved
--   IsLowCandidateRating           - the candidate rated the interview 2 or below
--   IsNegativeAIReportVerdict      - InterviewComments.AIVerdict = 'no'   (on this field 'no' is bad)
--   IsNegativeAITranscriptVerdict  - InterviewComments.AITranscriptVerdict = 'yes' (on this field 'yes' is bad)
-- RiskCategory: 3+ signals High, 2 Medium, 1 Low.
-- Rows: the most recent @RowLimit flagged interviews OR anything within @LookbackDays, whichever is more.
--
-- CandidateEmail is returned only so the application can look up the candidate's coded name
-- (CandidatePseudonym); it is never rendered.
CREATE PROCEDURE [dbo].[usp_GetInterviewAlignmentFlaggedInterviews]
    @InterviewId      INT,
    @InterviewerEmail VARCHAR(200),
    @TimeNow          DATETIME = NULL,
    @RowLimit         INT = 10,
    @LookbackDays     INT = 90
AS
BEGIN
    SET NOCOUNT ON;

    IF @TimeNow IS NULL SET @TimeNow = GETDATE();
    IF @RowLimit IS NULL OR @RowLimit <= 0 SET @RowLimit = 10;
    IF @LookbackDays IS NULL OR @LookbackDays < 0 SET @LookbackDays = 90;

    DECLARE @Email   VARCHAR(200) = LTRIM(RTRIM(REPLACE(ISNULL(@InterviewerEmail, ''), '"', '')));
    DECLARE @IRFCode VARCHAR(50);

    -- The gate: the caller must be this interview's interviewer.
    SELECT TOP 1 @IRFCode = im.IRFId
      FROM dbo.InterviewMeetings im
     WHERE im.id = @InterviewId
       AND LTRIM(RTRIM(REPLACE(ISNULL(im.interviewer, ''), '"', ''))) = @Email;

    IF @IRFCode IS NULL OR @Email = ''
    BEGIN
        -- Same shape, no rows.
        SELECT CAST(NULL AS INT) AS InterviewId, CAST(NULL AS DATETIME) AS InterviewDate, CAST(NULL AS VARCHAR(200)) AS CandidateEmail,
               CAST(NULL AS INT) AS SignalCount, CAST(NULL AS VARCHAR(10)) AS RiskCategory,
               CAST(NULL AS BIT) AS IsSuspiciousQC, CAST(NULL AS BIT) AS IsRedo, CAST(NULL AS BIT) AS IsLowCandidateRating,
               CAST(NULL AS BIT) AS IsNegativeAIReportVerdict, CAST(NULL AS BIT) AS IsNegativeAITranscriptVerdict,
               CAST(NULL AS INT) AS CandidateRating, CAST(NULL AS INT) AS AIReportQCAnalysisScore, CAST(NULL AS INT) AS AITranscriptAnalysisScore,
               CAST(NULL AS VARCHAR(600)) AS AIReasoning, CAST(NULL AS VARCHAR(400)) AS QCComment
         WHERE 1 = 0;
        RETURN;
    END

    ;WITH Past AS (
        SELECT
            im.id                                   AS InterviewId,
            im.interviewDate                        AS InterviewDate,
            LTRIM(RTRIM(im.candidateEmail))         AS CandidateEmail,
            CAST(CASE WHEN ISNULL(qc.IsSuspiciousappfound, 0) = 1 OR ISNULL(qc.Redflags, 0) > 0 THEN 1 ELSE 0 END AS BIT) AS IsSuspiciousQC,
            CAST(CASE WHEN ISNULL(im.RedoInterview, 0) = 1 OR ISNULL(ri.Approved, 0) = 1 THEN 1 ELSE 0 END AS BIT)      AS IsRedo,
            CAST(CASE WHEN cf.Rating IS NOT NULL AND cf.Rating <= 2 THEN 1 ELSE 0 END AS BIT)                            AS IsLowCandidateRating,
            CAST(CASE WHEN LOWER(ISNULL(ic.AIVerdict, '')) = 'no' THEN 1 ELSE 0 END AS BIT)                              AS IsNegativeAIReportVerdict,
            CAST(CASE WHEN LOWER(ISNULL(ic.AITranscriptVerdict, '')) = 'yes' THEN 1 ELSE 0 END AS BIT)                   AS IsNegativeAITranscriptVerdict,
            cf.Rating                               AS CandidateRating,
            ic.AIReportQCAnalysisScore              AS AIReportQCAnalysisScore,
            ic.AITranscriptAnalysisScore            AS AITranscriptAnalysisScore,
            LEFT(ic.AITranscriptAnalysis, 600)      AS AIReasoning,
            LEFT(ic.QCComment, 400)                 AS QCComment
          FROM dbo.InterviewMeetings im
          LEFT JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
          OUTER APPLY (SELECT TOP 1 q.IsSuspiciousappfound, q.Redflags
                         FROM dbo.Trans_QCFormReport q
                        WHERE q.InterviewId = im.id
                        ORDER BY q.id DESC) qc
          OUTER APPLY (SELECT TOP 1 r.Approved
                         FROM dbo.RedoInterviews r
                        WHERE r.InterviewId = im.id
                        ORDER BY r.id DESC) ri
          OUTER APPLY (SELECT TOP 1 f.Rating
                         FROM dbo.CandidateFeedbackDetails f
                        WHERE f.InterviewId = im.id
                        ORDER BY f.Id DESC) cf
         WHERE im.IRFId = @IRFCode
           AND LTRIM(RTRIM(REPLACE(ISNULL(im.interviewer, ''), '"', ''))) = @Email
           AND ISNULL(im.confirmationStatus, 0) = 1
           AND im.interviewDate < @TimeNow
           AND im.id <> @InterviewId
    ), Flagged AS (
        SELECT *,
               CAST(IsSuspiciousQC AS INT) + CAST(IsRedo AS INT) + CAST(IsLowCandidateRating AS INT)
             + CAST(IsNegativeAIReportVerdict AS INT) + CAST(IsNegativeAITranscriptVerdict AS INT) AS SignalCount
          FROM Past
    ), Ranked AS (
        SELECT *, ROW_NUMBER() OVER (ORDER BY InterviewDate DESC, InterviewId DESC) AS RowNo
          FROM Flagged
         WHERE SignalCount > 0
    )
    SELECT
        InterviewId,
        InterviewDate,
        CandidateEmail,
        SignalCount,
        CASE WHEN SignalCount >= 3 THEN 'High' WHEN SignalCount = 2 THEN 'Medium' ELSE 'Low' END AS RiskCategory,
        IsSuspiciousQC,
        IsRedo,
        IsLowCandidateRating,
        IsNegativeAIReportVerdict,
        IsNegativeAITranscriptVerdict,
        CandidateRating,
        AIReportQCAnalysisScore,
        AITranscriptAnalysisScore,
        AIReasoning,
        QCComment
      FROM Ranked
     WHERE RowNo <= @RowLimit
        OR InterviewDate >= DATEADD(DAY, -@LookbackDays, @TimeNow)
     ORDER BY InterviewDate DESC, InterviewId DESC;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 9. StoredProcedures/usp_AcknowledgeTrialOnboarding.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AcknowledgeTrialOnboarding'))
BEGIN
    DROP PROCEDURE dbo.usp_AcknowledgeTrialOnboarding
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- One-time trial onboarding acknowledgement from the interview alignment page. Same shape as
-- usp_updateRedmarkerAcknowledge, keyed on the signed-in interviewer's email rather than a uniqueid
-- taken from the request.
--
-- Only rows not yet acknowledged are touched, so the first acknowledgement timestamp and the policy
-- version it was given against survive any later call.
CREATE PROCEDURE [dbo].[usp_AcknowledgeTrialOnboarding]
    @InterviewerEmail VARCHAR(200),
    @PolicyVersion    VARCHAR(50),
    @CurrentDate      DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ip
       SET TrialOnboardingAcknowledged = 1,
           TrialOnboardingAcknowledgedOn = ISNULL(ip.TrialOnboardingAcknowledgedOn, @CurrentDate),
           TrialOnboardingPolicyVersion = ISNULL(ip.TrialOnboardingPolicyVersion, @PolicyVersion)
      FROM dbo.InterviewerProfile ip
      JOIN dbo.UserList ul ON ul.id = ip.UserId
     WHERE LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(@InterviewerEmail))
       AND ISNULL(ip.TrialOnboardingAcknowledged, 0) = 0;

    SELECT @@ROWCOUNT AS UpdatedRows;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 10. StoredProcedures/usp_SaveJoinGateOverride.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveJoinGateOverride'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveJoinGateOverride
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Records an interviewer choosing "join without confirming" on the interview alignment page, with the
-- briefing points that were still outstanding. The caller has already verified the interviewer owns
-- the interview (IsInterviewerForInterview).
CREATE PROCEDURE [dbo].[usp_SaveJoinGateOverride]
    @InterviewId             INT,
    @InterviewerEmail        VARCHAR(200),
    @OutstandingRequirements VARCHAR(500),
    @CurrentDate             DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InterviewJoinGateOverrides (InterviewId, InterviewerEmail, OutstandingRequirements, CreatedOn)
    VALUES (@InterviewId, LTRIM(RTRIM(@InterviewerEmail)), @OutstandingRequirements, @CurrentDate);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 11. StoredProcedures/usp_GetInterviewerCodeQuestions.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerCodeQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerCodeQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- "Your questions" on the interview alignment page: the signed-in interviewer's own TechSkillComments
-- rows for the interview's channel - the same rows IRF/SampleQuestions lets them edit - with the code
-- session already minted for this interview, if any. Unlike usp_GetTechSkillQuestionsByUserId there is
-- no admin widening: this list is always the interviewer's own, because only their own rows can be
-- minted. Client samples (SkillChannelSampleQuestions) are deliberately not part of it.
CREATE PROCEDURE [dbo].[usp_GetInterviewerCodeQuestions]
    @InterviewId      INT,
    @InterviewerEmail VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Email VARCHAR(200) = LTRIM(RTRIM(REPLACE(ISNULL(@InterviewerEmail, ''), '"', '')));
    DECLARE @IrfIntId INT;
    DECLARE @UserId INT;

    SELECT @IrfIntId = irf.id
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.id = @InterviewId;

    SELECT TOP 1 @UserId = ul.id
    FROM dbo.UserList ul
    WHERE LTRIM(RTRIM(ul.EmailId)) = @Email
    ORDER BY ul.id;

    SELECT tsc.id AS QuestionId,
           tsc.Comment,
           tsc.SkillName,
           CAST(ISNULL(tsc.IsCodingQuestion, 0) AS BIT) AS IsCodingQuestion,
           tsc.CodingTimeLimitInMinutes,
           ics.Token,
           ics.RunCount,
           ics.LastRunStatus,
           ics.OpenedOn,
           ics.SubmittedOn
    FROM dbo.TechSkillComments tsc
    LEFT JOIN dbo.InterviewCodeSessions ics
        ON ics.InterviewId = @InterviewId AND ics.QuestionId = tsc.id
    WHERE tsc.IRFId = @IrfIntId
      AND tsc.UserId = @UserId
      AND @UserId IS NOT NULL
    ORDER BY tsc.id ASC;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 12. StoredProcedures/usp_CreateInterviewCodeSession.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CreateInterviewCodeSession'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateInterviewCodeSession
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Mints a code session for one of the interviewer's own questions, from the interview alignment page.
-- One request does all of it, in one transaction:
--   1. @QuestionId = 0: the typed question is written to TechSkillComments for this interviewer and
--      channel first, so it is in their list on the next interview for the channel.
--      @QuestionId > 0: the row must belong to this interviewer on this channel - never another
--      interviewer's question and never a client sample (SkillChannelSampleQuestions is not read).
--   2. the row is marked IsCodingQuestion = 1 with its time limit (no question-gist requirement).
--   3. a session is created for (interview, question), or the existing one is returned.
--
-- The interviewer and channel are resolved from the interview and the signed-in email, never taken
-- from the request. Status: 'ok', 'not_interviewer', 'not_owner', 'empty_question'.
CREATE PROCEDURE [dbo].[usp_CreateInterviewCodeSession]
    @InterviewId      INT,
    @InterviewerEmail VARCHAR(200),
    @QuestionId       INT,
    @QuestionText     NVARCHAR(MAX),
    @SkillName        VARCHAR(500),
    @TimeLimit        INT,
    @Token            UNIQUEIDENTIFIER,
    @CurrentDate      DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Email VARCHAR(200) = LTRIM(RTRIM(REPLACE(ISNULL(@InterviewerEmail, ''), '"', '')));
    DECLARE @IrfIntId INT;
    DECLARE @UserId INT;

    SELECT @IrfIntId = irf.id
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.id = @InterviewId
      AND LTRIM(RTRIM(REPLACE(ISNULL(im.interviewer, ''), '"', ''))) = @Email;

    SELECT TOP 1 @UserId = ul.id
    FROM dbo.UserList ul
    WHERE LTRIM(RTRIM(ul.EmailId)) = @Email
    ORDER BY ul.id;

    IF @IrfIntId IS NULL OR @UserId IS NULL OR @Email = ''
    BEGIN
        SELECT CAST('not_interviewer' AS VARCHAR(20)) AS Status;
        RETURN;
    END

    BEGIN TRAN;

    IF ISNULL(@QuestionId, 0) = 0
    BEGIN
        IF LTRIM(RTRIM(ISNULL(@QuestionText, N''))) = N''
        BEGIN
            ROLLBACK TRAN;
            SELECT CAST('empty_question' AS VARCHAR(20)) AS Status;
            RETURN;
        END

        INSERT INTO dbo.TechSkillComments (IRFId, Skill, Comment, UserId, SkillName, IsCodingQuestion, CodingTimeLimitInMinutes)
        VALUES (@IrfIntId, NULL, LTRIM(RTRIM(@QuestionText)), @UserId, @SkillName, 1, @TimeLimit);

        SET @QuestionId = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.TechSkillComments tsc
                       WHERE tsc.id = @QuestionId AND tsc.IRFId = @IrfIntId AND tsc.UserId = @UserId)
        BEGIN
            ROLLBACK TRAN;
            SELECT CAST('not_owner' AS VARCHAR(20)) AS Status;
            RETURN;
        END

        UPDATE dbo.TechSkillComments
           SET IsCodingQuestion = 1,
               CodingTimeLimitInMinutes = ISNULL(@TimeLimit, CodingTimeLimitInMinutes)
         WHERE id = @QuestionId;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.InterviewCodeSessions WITH (UPDLOCK, HOLDLOCK)
                   WHERE InterviewId = @InterviewId AND QuestionId = @QuestionId)
    BEGIN
        -- The snapshot keeps the text exactly as typed when the question was just written (TechSkillComments.Comment
        -- is varchar and would lose non-Latin characters); an existing row is snapshotted as it stands.
        INSERT INTO dbo.InterviewCodeSessions (InterviewId, QuestionId, QuestionText, Token, CodingTimeLimitInMinutes, CreatedBy, CreatedOn)
        SELECT @InterviewId, tsc.id,
               COALESCE(NULLIF(LTRIM(RTRIM(@QuestionText)), N''), CAST(tsc.Comment AS NVARCHAR(MAX)), N''),
               @Token, tsc.CodingTimeLimitInMinutes, @Email, @CurrentDate
        FROM dbo.TechSkillComments tsc
        WHERE tsc.id = @QuestionId;
    END

    COMMIT TRAN;

    SELECT CAST('ok' AS VARCHAR(20)) AS Status,
           ics.Id, ics.InterviewId, ics.QuestionId, ics.QuestionText, ics.Token, ics.CodingTimeLimitInMinutes,
           ics.LanguageId, ics.RunCount, ics.LastRunStatus, ics.CreatedOn, ics.OpenedOn, ics.SubmittedOn
    FROM dbo.InterviewCodeSessions ics
    WHERE ics.InterviewId = @InterviewId AND ics.QuestionId = @QuestionId;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 13. StoredProcedures/usp_GetInterviewCodeSessions.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewCodeSessions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewCodeSessions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Every code session minted for an interview, oldest first. Read by the interview alignment page (the
-- interviewer's own interview, ownership checked by the caller) and by the client report views, which
-- render it under their own access control. QuestionText is the snapshot taken when the session was
-- minted, so the report shows what the candidate was actually asked.
CREATE PROCEDURE [dbo].[usp_GetInterviewCodeSessions]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ics.Id, ics.InterviewId, ics.QuestionId, ics.QuestionText, ics.Token, ics.LanguageId,
           ics.SubmittedSource, ics.LastRunSource, ics.LastRunOutput, ics.LastRunStatus, ics.RunCount,
           ics.CodingTimeLimitInMinutes, ics.CreatedBy, ics.CreatedOn, ics.OpenedOn, ics.SubmittedOn
    FROM dbo.InterviewCodeSessions ics
    WHERE ics.InterviewId = @InterviewId
    ORDER BY ics.CreatedOn ASC, ics.Id ASC;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 14. StoredProcedures/usp_GetInterviewCodeSessionByToken.sql
-- -----------------------------------------------------------------------------------------------------
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

GO

-- -----------------------------------------------------------------------------------------------------
-- 15. StoredProcedures/usp_MarkInterviewCodeSessionOpened.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MarkInterviewCodeSessionOpened'))
BEGIN
    DROP PROCEDURE dbo.usp_MarkInterviewCodeSessionOpened
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- First time the candidate opens the link starts the question's clock. Later opens never move it.
CREATE PROCEDURE [dbo].[usp_MarkInterviewCodeSessionOpened]
    @Token       UNIQUEIDENTIFIER,
    @CurrentDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InterviewCodeSessions
       SET OpenedOn = @CurrentDate
     WHERE Token = @Token
       AND OpenedOn IS NULL;

    SELECT OpenedOn FROM dbo.InterviewCodeSessions WHERE Token = @Token;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 16. StoredProcedures/usp_SaveInterviewCodeSessionRun.sql
-- -----------------------------------------------------------------------------------------------------
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveInterviewCodeSessionRun'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewCodeSessionRun
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Persists the latest run of a code session: the source that was run, what the runner returned
-- (already capped by CodeExecutionService) and the backend status. The runner itself stays read-only;
-- this is the session's own record of what came back.
CREATE PROCEDURE [dbo].[usp_SaveInterviewCodeSessionRun]
    @Token       UNIQUEIDENTIFIER,
    @LanguageId  VARCHAR(40),
    @Source      NVARCHAR(MAX),
    @Output      NVARCHAR(MAX),
    @Status      VARCHAR(40),
    @CurrentDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InterviewCodeSessions
       SET LanguageId = @LanguageId,
           LastRunSource = @Source,
           LastRunOutput = @Output,
           LastRunStatus = @Status,
           RunCount = RunCount + 1,
           OpenedOn = ISNULL(OpenedOn, @CurrentDate)
     WHERE Token = @Token;

    SELECT RunCount FROM dbo.InterviewCodeSessions WHERE Token = @Token;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 17. StoredProcedures/usp_SubmitInterviewCodeSession.sql
-- -----------------------------------------------------------------------------------------------------
 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SubmitInterviewCodeSession'))
BEGIN
    DROP PROCEDURE dbo.usp_SubmitInterviewCodeSession
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- The candidate's submitted answer for a code session. A later submit (before the token expires)
-- replaces the earlier one; the report shows the latest submission next to the last run's output.
CREATE PROCEDURE [dbo].[usp_SubmitInterviewCodeSession]
    @Token       UNIQUEIDENTIFIER,
    @LanguageId  VARCHAR(40),
    @Source      NVARCHAR(MAX),
    @CurrentDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InterviewCodeSessions
       SET LanguageId = @LanguageId,
           SubmittedSource = @Source,
           SubmittedOn = @CurrentDate,
           OpenedOn = ISNULL(OpenedOn, @CurrentDate)
     WHERE Token = @Token;

    SELECT SubmittedOn FROM dbo.InterviewCodeSessions WHERE Token = @Token;
END
GO

GO

-- -----------------------------------------------------------------------------------------------------
-- 18. StoredProcedures/usp_AddTechSkillComments.sql
-- -----------------------------------------------------------------------------------------------------
/****** Object:  StoredProcedure [dbo].[usp_AddTechSkillComments]    Script Date: 12/22/2021 5:45:42 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTechSkillComments'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTechSkillComments
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  

Create PROCEDURE [dbo].[usp_AddTechSkillComments]    
(    
   @TechSkillCommentsBulkData AS [dbo].[TechSkillComments_TableTypeV2] READONLY,--TechSkillComments_TableType  
   @IRFId int,
   @userId int
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran 
  
  -- Code sessions: this save deletes and re-inserts the rows, which would drop the IsCodingQuestion flag
  -- an interviewer set from the interview alignment page and orphan the sessions minted against the old
  -- ids. Remember the coding rows (keyed by owner and normalised question text) so both can be carried
  -- over to the re-inserted rows below. Nothing else about this save changes.
  declare @CodingRows table (OldId int, UserId int, CommentKey varchar(max), IsCodingQuestion bit, CodingTimeLimitInMinutes int)
  insert into @CodingRows (OldId, UserId, CommentKey, IsCodingQuestion, CodingTimeLimitInMinutes)
  select tsc.id, tsc.UserId,
         LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(tsc.Comment, CHAR(13), ''), CHAR(10), ''), CHAR(9), ''))),
         tsc.IsCodingQuestion, tsc.CodingTimeLimitInMinutes
  from TechSkillComments tsc
  where tsc.IRFId = @IRFId and ISNULL(tsc.IsCodingQuestion, 0) = 1

  declare @profileId int
  set @profileId = (select idProfile from UserList where id = @userId)
  if(@profileId = 1)--Admin
  begin
	delete from TechSkillComments where IRFId =@IRFId

   INSERT INTO [dbo].[TechSkillComments](    
    [IRFId] ,    
    [Skill],    
    [Comment],  
    [UserId],
	[SkillName])    
     
    
   SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.UserId, tscbd.SkillName FROM @TechSkillCommentsBulkData tscbd

  end
  else
  begin
	delete from TechSkillComments where IRFId =@IRFId and UserId =@userId

   INSERT INTO [dbo].[TechSkillComments](    
    [IRFId] ,    
    [Skill],    
    [Comment],  
    [UserId],
	[SkillName])    
     
    
   SELECT tscbd.IRFId, tscbd.Skill, LTRIM(RTRIM(
        REPLACE(
            REPLACE(
                REPLACE(tscbd.Comment, CHAR(13), ''),  -- \r
            CHAR(10), ''),                             -- \n
        CHAR(9), '')                                   -- \t
    )) AS Comment, tscbd.UserId, tscbd.SkillName FROM @TechSkillCommentsBulkData tscbd
  end

  if exists (select 1 from @CodingRows)
  begin
	-- Carry the coding flag onto the re-inserted row with the same owner and question text.
	update t
	   set IsCodingQuestion = c.IsCodingQuestion,
	       CodingTimeLimitInMinutes = c.CodingTimeLimitInMinutes
	  from TechSkillComments t
	  join @CodingRows c
	    on ISNULL(c.UserId, -1) = ISNULL(t.UserId, -1)
	   and c.CommentKey = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(t.Comment, CHAR(13), ''), CHAR(10), ''), CHAR(9), '')))
	 where t.IRFId = @IRFId
	   and ISNULL(t.IsCodingQuestion, 0) = 0

	-- Re-point sessions whose question row was just deleted at its re-inserted copy. One session per
	-- (interview, question) is kept unique: a session is only moved when the target is free.
	;with Remap as (
		select c.OldId, MIN(t.id) as NewId
		  from @CodingRows c
		  join TechSkillComments t
		    on t.IRFId = @IRFId
		   and ISNULL(c.UserId, -1) = ISNULL(t.UserId, -1)
		   and c.CommentKey = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(t.Comment, CHAR(13), ''), CHAR(10), ''), CHAR(9), '')))
		 where not exists (select 1 from TechSkillComments x where x.id = c.OldId)
		 group by c.OldId
	), Moves as (
		select ics.Id, r.NewId,
		       ROW_NUMBER() over (partition by ics.InterviewId, r.NewId order by ics.Id) as rn
		  from InterviewCodeSessions ics
		  join Remap r on r.OldId = ics.QuestionId
		 where not exists (select 1 from InterviewCodeSessions y
		                    where y.InterviewId = ics.InterviewId and y.QuestionId = r.NewId)
	)
	update ics
	   set QuestionId = m.NewId
	  from InterviewCodeSessions ics
	  join Moves m on m.Id = ics.Id
	 where m.rn = 1
  end


  
  commit tran  
 end try  
 begin catch  
  
  rollback transaction  
  
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
   @Severity int = ERROR_SEVERITY(),  
   @State smallint = ERROR_STATE()  
   
  RAISERROR (@Message, @Severity, @State)  
   
 end catch  
  
    
END    
GO



GO

PRINT 'Interview alignment + code sessions: deployment complete.';
GO
