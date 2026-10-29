-- =============================================================================
-- PlivoCX campaigns: everything the campaign changes need, in one run (Oct 2026).
--   - tables: agent registry + Provider / ProviderAgentId, campaign + Provider,
--     candidate + DialLockUntil, call log + CampaignId
--   - procedures: campaign list (Provider), candidate table + due candidates (in-flight window),
--     IRF picker (meeting time), Not-scheduled source (fast)
--   - the three ElevenLabs agents created by Dashboard/CreateElevenLabsCampaignAgents
-- Every part is idempotent (guarded ALTERs, DROP + CREATE procedures), so it is safe to re-run.
-- Generated from the individual scripts in Risebird.DB - edit those, not this file.
-- =============================================================================


-- ---------------------------------------------------------------------------
-- Tables/PlivoCXAgent.sql
-- ---------------------------------------------------------------------------
-- PlivoCX campaigns: the agent registry - which Plivo AgentFlow a campaign runs.
--
-- An agent is a Plivo AgentFlow addressed by its trigger URL, and the flow is the behaviour:
-- Schedule collects a slot and books, Reschedule moves a meeting, Confirm attendance only asks
-- the candidate to confirm (no slot), and the interviewer agents call interviewers. Every agent
-- is dialled through IPlivoCXService.TriggerByUrlAsync(TriggerUrl, request).
--
-- Rows are seeded by the application (PlivoCXCampaignService.EnsureAgentsSeededAsync) from the
-- PlivoCX config keys - TriggerEndpointUrl, RescheduleTriggerEndpointUrl,
-- ConfirmTriggerEndpointUrl, AvailabilityTriggerEndpointUrl, OnboardingTriggerEndpointUrl - one
-- row per purpose whose URL is set, so each environment gets its own URLs. Ops can add more
-- rows as new flows ship, without a deploy.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'PlivoCXAgent')
BEGIN
CREATE TABLE [dbo].[PlivoCXAgent](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	-- "Schedule", "Reschedule", "Confirm attendance", ...
	[Name] [varchar](120) NOT NULL,
	-- Shown in the agent dropdown, e.g. "collect availability & book".
	[Description] [varchar](200) NULL,
	-- Plivo AgentFlow trigger URL.
	[TriggerUrl] [varchar](400) NOT NULL,
	-- schedule | reschedule | confirm | availability | onboard
	[Purpose] [varchar](20) NOT NULL,
	-- Candidate | Interviewer
	[TargetType] [varchar](20) NOT NULL,
	-- 0 for a confirm-only agent: the hangup webhook skips booking for it.
	[CollectsSlot] [bit] NOT NULL CONSTRAINT [DF_PlivoCXAgent_CollectsSlot] DEFAULT (1),
	-- Voice | WhatsApp
	[Channel] [varchar](12) NOT NULL CONSTRAINT [DF_PlivoCXAgent_Channel] DEFAULT ('Voice'),
	[IsActive] [bit] NOT NULL CONSTRAINT [DF_PlivoCXAgent_IsActive] DEFAULT (1),
	[CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_PlivoCXAgent_CreatedAt] DEFAULT (GETUTCDATE()),
 CONSTRAINT [PK_PlivoCXAgent] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
END
GO

-- Which voice platform runs the agent: Plivo (AgentFlow, dialled through TriggerUrl) | ElevenLabs
-- (Agents Platform, dialled by ProviderAgentId; TriggerUrl is empty) | Vapi (not built yet).
-- Existing rows are Plivo.
IF COL_LENGTH('dbo.PlivoCXAgent', 'Provider') IS NULL
BEGIN
	ALTER TABLE [dbo].[PlivoCXAgent] ADD [Provider] [varchar](20) NOT NULL
		CONSTRAINT [DF_PlivoCXAgent_Provider] DEFAULT ('Plivo');
END
GO

-- The provider's own id for the agent, e.g. an ElevenLabs agent_id. Written by
-- Dashboard/CreateElevenLabsCampaignAgents, which creates (or updates) the ElevenLabs agents.
IF COL_LENGTH('dbo.PlivoCXAgent', 'ProviderAgentId') IS NULL
BEGIN
	ALTER TABLE [dbo].[PlivoCXAgent] ADD [ProviderAgentId] [varchar](100) NULL;
END
GO

-- ---------------------------------------------------------------------------
-- Tables/PlivoCXCampaign.sql
-- ---------------------------------------------------------------------------
-- PlivoCX campaigns: one operator-run calling campaign (AI scheduling, rescheduling or
-- attendance confirmation) with its calling policy inlined.
--
-- Status: Draft -> Running (Start automatic) <-> Paused -> Completed. Draft and Paused campaigns
-- are only dialled by Call now / per-row Call; the PlivoCXCampaignScheduler timer dials Running
-- ones inside the policy window and marks them Completed when nobody is left Pending or EndsOn
-- has passed.
--
-- A campaign is sourced from one IRF (IRFId set) or from a centralized list that can span many
-- IRFs (IRFId NULL, IRFName = "Not-scheduled list" etc.); each candidate row keeps its own IRF.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'PlivoCXCampaign')
BEGIN
CREATE TABLE [dbo].[PlivoCXCampaign](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	-- IRF.id when sourced By IRF.
	[IRFId] [int] NULL,
	-- IRF.IRFId business string, or the list name ("Not-scheduled list", "Reschedule list", ...).
	[IRFName] [nvarchar](100) NULL,
	-- Client name, or a summary such as "12 clients" for a list-sourced campaign.
	[ClientName] [nvarchar](200) NULL,
	-- IRF | NotScheduled | Reschedule | Unconfirmed
	[SourceType] [varchar](20) NOT NULL CONSTRAINT [DF_PlivoCXCampaign_SourceType] DEFAULT ('IRF'),
	-- PlivoCXAgent.Id - which AI flow runs.
	[AgentId] [int] NOT NULL,
	-- Copied from the agent: schedule | reschedule | confirm | ...
	[CallPurpose] [varchar](20) NOT NULL,
	-- Draft | Running | Paused | Completed
	[Status] [varchar](20) NOT NULL CONSTRAINT [DF_PlivoCXCampaign_Status] DEFAULT ('Draft'),

	-- Calling policy
	[WindowOpens] [time](0) NOT NULL CONSTRAINT [DF_PlivoCXCampaign_WindowOpens] DEFAULT ('09:00'),
	[WindowCloses] [time](0) NOT NULL CONSTRAINT [DF_PlivoCXCampaign_WindowCloses] DEFAULT ('20:00'),
	-- IANA or Windows time zone id.
	[TimeZone] [varchar](60) NOT NULL CONSTRAINT [DF_PlivoCXCampaign_TimeZone] DEFAULT ('Asia/Kolkata'),
	[AttemptsPerDay] [int] NOT NULL CONSTRAINT [DF_PlivoCXCampaign_AttemptsPerDay] DEFAULT (3),
	[TotalAttempts] [int] NOT NULL CONSTRAINT [DF_PlivoCXCampaign_TotalAttempts] DEFAULT (6),
	[MinGapMinutes] [int] NOT NULL CONSTRAINT [DF_PlivoCXCampaign_MinGapMinutes] DEFAULT (180),
	[ConcurrentCalls] [int] NOT NULL CONSTRAINT [DF_PlivoCXCampaign_ConcurrentCalls] DEFAULT (5),
	[StartsOn] [date] NULL,
	[EndsOn] [date] NULL,

	[CreatedBy] [varchar](150) NULL,
	[CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_PlivoCXCampaign_CreatedAt] DEFAULT (GETUTCDATE()),
	[UpdatedAt] [datetime] NULL,
 CONSTRAINT [PK_PlivoCXCampaign] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PlivoCXCampaign_Status' AND object_id = OBJECT_ID(N'dbo.PlivoCXCampaign'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_PlivoCXCampaign_Status] ON [dbo].[PlivoCXCampaign] ([Status])
END
GO

-- Which voice platform the campaign calls through: Plivo | ElevenLabs | Vapi. Chosen on Create;
-- the campaign's agent must belong to the same provider. Existing campaigns are Plivo.
IF COL_LENGTH('dbo.PlivoCXCampaign', 'Provider') IS NULL
BEGIN
	ALTER TABLE [dbo].[PlivoCXCampaign] ADD [Provider] [varchar](20) NOT NULL
		CONSTRAINT [DF_PlivoCXCampaign_Provider] DEFAULT ('Plivo');
END
GO

-- ---------------------------------------------------------------------------
-- Tables/PlivoCXCampaignCandidate.sql
-- ---------------------------------------------------------------------------
-- PlivoCX campaigns: one candidate in a campaign - the per-candidate calling state machine.
--
-- State: Pending (still to be reached) -> Booked (slot booked, or attendance confirmed for a
-- confirm-only agent) | NotBooked (answered but no booking) | Cold (no valid number, or every
-- attempt used up). The hangup webhook drives the transitions; Mark... on the detail page
-- overrides them by hand.
--
-- Times (LastCallAt, NextAttemptAt, CreatedAt) are UTC.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'PlivoCXCampaignCandidate')
BEGIN
CREATE TABLE [dbo].[PlivoCXCampaignCandidate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CampaignId] [int] NOT NULL,
	[CandidateId] [int] NOT NULL,
	[CandidateName] [nvarchar](200) NULL,
	[CandidateEmail] [nvarchar](200) NULL,
	[CandidatePhone] [varchar](30) NULL,

	-- Interview context, per row because a list-sourced campaign spans IRFs.
	[IRFId] [int] NULL,
	[IRFName] [nvarchar](100) NULL,
	[JobTitle] [nvarchar](255) NULL,
	[ClientName] [nvarchar](255) NULL,
	[InterviewId] [int] NULL,
	-- For reschedule / confirm agents: the meeting being moved or confirmed, e.g. "07 Mar 2026".
	[OriginalInterviewDate] [nvarchar](50) NULL,

	-- Pending | Booked | NotBooked | Cold
	[State] [varchar](20) NOT NULL CONSTRAINT [DF_PlivoCXCampaignCandidate_State] DEFAULT ('Pending'),
	[Attempts] [int] NOT NULL CONSTRAINT [DF_PlivoCXCampaignCandidate_Attempts] DEFAULT (0),
	[LastCallAt] [datetime] NULL,
	-- Earliest time the next attempt may be dialled (NULL = now).
	[NextAttemptAt] [datetime] NULL,
	-- interested | uninterested | cant_say, or a call outcome such as no-answer.
	[LastDisposition] [varchar](30) NULL,
	[BookedSlot] [nvarchar](60) NULL,
	-- PlivoCXCallLogs.Id of the latest call, for the Call detail link.
	[LastCallLogId] [int] NULL,
	[CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_PlivoCXCampaignCandidate_CreatedAt] DEFAULT (GETUTCDATE()),
	[UpdatedAt] [datetime] NULL,
 CONSTRAINT [PK_PlivoCXCampaignCandidate] PRIMARY KEY CLUSTERED ([Id] ASC),
 CONSTRAINT [UQ_PlivoCXCampaignCandidate_Campaign_Candidate] UNIQUE ([CampaignId], [CandidateId])
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PlivoCXCampaignCandidate_Candidate_State' AND object_id = OBJECT_ID(N'dbo.PlivoCXCampaignCandidate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_PlivoCXCampaignCandidate_Candidate_State]
	ON [dbo].[PlivoCXCampaignCandidate] ([CandidateId], [State]) INCLUDE ([CampaignId])
END
GO

-- Dial lock: set atomically just before a call is placed and cleared once the call is logged, so
-- Call now, a per-row Call and the scheduler can never dial the same candidate at the same time.
-- A lock older than its expiry (a crash mid-dial) no longer blocks.
IF COL_LENGTH('dbo.PlivoCXCampaignCandidate', 'DialLockUntil') IS NULL
BEGIN
	ALTER TABLE [dbo].[PlivoCXCampaignCandidate] ADD [DialLockUntil] [datetime] NULL;
END
GO

-- ---------------------------------------------------------------------------
-- Migrations/Add_PlivoCXCallLogs_CampaignId.sql
-- ---------------------------------------------------------------------------
-- PlivoCX campaigns: ties every call log to the campaign that dialled it (NULL for timer-job
-- and one-off calls). The application writes it with a separate UPDATE after the INSERT, so
-- calls keep logging on a database where this script has not run yet.
IF COL_LENGTH('dbo.PlivoCXCallLogs', 'CampaignId') IS NULL
BEGIN
	ALTER TABLE [dbo].[PlivoCXCallLogs] ADD [CampaignId] [int] NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PlivoCXCallLogs_CampaignId' AND object_id = OBJECT_ID(N'dbo.PlivoCXCallLogs'))
BEGIN
	CREATE NONCLUSTERED INDEX [IX_PlivoCXCallLogs_CampaignId]
		ON [dbo].[PlivoCXCallLogs] ([CampaignId], [CandidateId])
		INCLUDE ([CallStatus], [CreatedAt])
		WHERE [CampaignId] IS NOT NULL;
END
GO

-- ---------------------------------------------------------------------------
-- StoredProcedures/usp_GetPlivoCXCampaigns.sql
-- ---------------------------------------------------------------------------
-- =============================================
-- PlivoCX campaigns: the Campaigns list (/PlivoCXCampaign).
-- One row per campaign with its agent and the candidate states rolled up.
--   @Search : matches campaign name, IRF / list name, client or agent (NULL = all)
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaigns'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaigns
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaigns]
    @Search NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), '');

    SELECT
        pc.Id,
        pc.Name,
        pc.IRFId,
        pc.IRFName,
        pc.ClientName,
        pc.SourceType,
        pc.Provider,
        pc.AgentId,
        a.Name                     AS AgentName,
        pc.CallPurpose,
        pc.[Status],
        pc.CreatedBy,
        pc.CreatedAt,
        ISNULL(s.Candidates, 0)    AS Candidates,
        ISNULL(s.Booked, 0)        AS Booked,
        ISNULL(s.NotBooked, 0)     AS NotBooked,
        ISNULL(s.Cold, 0)          AS Cold,
        ISNULL(s.Pending, 0)       AS Pending
    FROM      dbo.PlivoCXCampaign pc
    LEFT JOIN dbo.PlivoCXAgent    a ON a.Id = pc.AgentId
    OUTER APPLY (
        SELECT
            COUNT(*)                                                  AS Candidates,
            SUM(CASE WHEN cc.[State] = 'Booked'    THEN 1 ELSE 0 END) AS Booked,
            SUM(CASE WHEN cc.[State] = 'NotBooked' THEN 1 ELSE 0 END) AS NotBooked,
            SUM(CASE WHEN cc.[State] = 'Cold'      THEN 1 ELSE 0 END) AS Cold,
            SUM(CASE WHEN cc.[State] = 'Pending'   THEN 1 ELSE 0 END) AS Pending
        FROM dbo.PlivoCXCampaignCandidate cc
        WHERE cc.CampaignId = pc.Id
    ) s
    WHERE @Search IS NULL
       OR pc.Name       LIKE '%' + @Search + '%'
       OR pc.IRFName    LIKE '%' + @Search + '%'
       OR pc.ClientName LIKE '%' + @Search + '%'
       OR a.Name        LIKE '%' + @Search + '%'
    ORDER BY pc.Id DESC;
END
GO

-- ---------------------------------------------------------------------------
-- StoredProcedures/usp_GetPlivoCXCampaignCandidates.sql
-- ---------------------------------------------------------------------------
-- =============================================
-- PlivoCX campaigns: the candidate table on the campaign detail page.
--   @CampaignId : the campaign
--
-- Result 1 : every candidate row, with the latest call's recording and status.
-- Result 2 : call statistics for the tiles - total calls that finished (anything that is not
--            still initiated / triggered) and how many of them were answered (completed).
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaignCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaignCandidates
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaignCandidates]
    @CampaignId INT,
    -- Calls initiated / triggered within this many minutes count as in flight (PlivoCX:InFlightTimeoutMinutes).
    @InFlightMinutes INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cc.Id,
        cc.CampaignId,
        cc.CandidateId,
        cc.CandidateName,
        cc.CandidateEmail,
        cc.CandidatePhone,
        cc.IRFId,
        cc.IRFName,
        cc.JobTitle,
        cc.ClientName,
        cc.InterviewId,
        cc.OriginalInterviewDate,
        cc.[State],
        cc.Attempts,
        cc.LastCallAt,
        cc.NextAttemptAt,
        cc.LastDisposition,
        cc.BookedSlot,
        cc.LastCallLogId,
        cc.CreatedAt,
        cc.UpdatedAt,
        l.RecordingUrl  AS LastRecordingUrl,
        l.CallStatus    AS LastCallStatus
    FROM      dbo.PlivoCXCampaignCandidate cc
    LEFT JOIN dbo.PlivoCXCallLogs          l ON l.Id = cc.LastCallLogId
    WHERE cc.CampaignId = @CampaignId
    ORDER BY
        CASE cc.[State] WHEN 'Pending' THEN 0 WHEN 'Booked' THEN 1 WHEN 'NotBooked' THEN 2 ELSE 3 END,
        cc.LastCallAt DESC,
        cc.Id;

    SELECT
        SUM(CASE WHEN l.CallStatus NOT IN ('initiated', 'triggered') THEN 1 ELSE 0 END) AS FinishedCalls,
        SUM(CASE WHEN l.CallStatus = 'completed' THEN 1 ELSE 0 END)                      AS AnsweredCalls,
        SUM(CASE WHEN l.CallStatus IN ('initiated', 'triggered')
                  AND l.CreatedAt >= DATEADD(MINUTE, -@InFlightMinutes, GETUTCDATE()) THEN 1 ELSE 0 END) AS InFlightCalls
    FROM dbo.PlivoCXCallLogs l
    WHERE l.CampaignId = @CampaignId;
END
GO

-- ---------------------------------------------------------------------------
-- StoredProcedures/usp_GetDuePlivoCXCampaignCandidates.sql
-- ---------------------------------------------------------------------------
-- =============================================
-- PlivoCX campaigns: the candidates of one campaign that may be dialled now.
-- Used by Call now and by the PlivoCXCampaignScheduler timer.
--
--   @CampaignId  : the campaign
--   @Take        : how many to return (the free concurrent-call slots)
--   @NowUtc      : current UTC time
--   @DayStartUtc : start of "today" in the campaign's time zone, as UTC - for Attempts/day
--
-- Due = State 'Pending'
--   AND NextAttemptAt is unset or has passed (Min gap)
--   AND Attempts < TotalAttempts
--   AND calls to the candidate from this campaign since @DayStartUtc < AttemptsPerDay
--   AND no other dial holds the candidate (DialLockUntil unset or expired)
--   AND no call to the candidate is still connecting (initiated / triggered within the last
--       @InFlightMinutes - older ones never got a webhook and no longer block).
-- The calling window is checked by the scheduler, not here: Call now ignores it.
-- Fewest attempts first, so fresh candidates go before retries.
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDuePlivoCXCampaignCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDuePlivoCXCampaignCandidates
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDuePlivoCXCampaignCandidates]
    @CampaignId  INT,
    @Take        INT,
    @NowUtc      DATETIME,
    @DayStartUtc DATETIME,
    -- A call still initiated / triggered after this many minutes never got its hangup webhook
    -- and no longer blocks the candidate. The app passes PlivoCX:InFlightTimeoutMinutes.
    @InFlightMinutes INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InFlightFrom DATETIME = DATEADD(MINUTE, -@InFlightMinutes, @NowUtc);

    SELECT TOP (@Take)
        cc.Id,
        cc.CampaignId,
        cc.CandidateId,
        cc.CandidateName,
        cc.CandidateEmail,
        cc.CandidatePhone,
        cc.IRFId,
        cc.IRFName,
        cc.JobTitle,
        cc.ClientName,
        cc.InterviewId,
        cc.OriginalInterviewDate,
        cc.[State],
        cc.Attempts,
        cc.LastCallAt,
        cc.NextAttemptAt,
        cc.LastDisposition,
        cc.BookedSlot,
        cc.LastCallLogId
    FROM       dbo.PlivoCXCampaignCandidate cc
    INNER JOIN dbo.PlivoCXCampaign          pc ON pc.Id = cc.CampaignId
    WHERE cc.CampaignId = @CampaignId
      AND cc.[State] = 'Pending'
      AND (cc.NextAttemptAt IS NULL OR cc.NextAttemptAt <= @NowUtc)
      AND cc.Attempts < pc.TotalAttempts
      AND (
            SELECT COUNT(*)
            FROM dbo.PlivoCXCallLogs l
            WHERE l.CampaignId  = cc.CampaignId
              AND l.CandidateId = cc.CandidateId
              AND l.CreatedAt  >= @DayStartUtc
          ) < pc.AttemptsPerDay
      AND (cc.DialLockUntil IS NULL OR cc.DialLockUntil < @NowUtc)
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.PlivoCXCallLogs l
            WHERE l.CandidateId = cc.CandidateId
              AND l.CallStatus IN ('initiated', 'triggered')
              AND l.CreatedAt >= @InFlightFrom
          )
    ORDER BY cc.Attempts ASC, ISNULL(cc.NextAttemptAt, '19000101') ASC, cc.Id ASC;
END
GO

-- ---------------------------------------------------------------------------
-- StoredProcedures/usp_GetPlivoCXCampaignPickerForIRF.sql
-- ---------------------------------------------------------------------------
-- =============================================
-- PlivoCX campaigns: the "By IRF" candidate picker on Create campaign.
-- Every candidate of the IRF with the facts the picker labels them by; whether a candidate is
-- callable for the chosen agent comes from the eligibility procedures
-- (usp_GetEligibleCandidatesForPlivoCXCalls / ...Reschedule with @IRFId), merged in code.
--
--   @IRFId  : IRF.id
--   @NowIST : current IST time (InterviewMeetings.interviewDate is IST)
--
-- HasMeeting           : a future meeting that is not cancelled (confirmationStatus <> '2')
-- UpcomingInterviewId / UpcomingInterviewDate : the next such meeting (for confirm agents), with its time
-- LastInterviewDate    : the latest past meeting (context for reschedule agents)
-- IsCold               : Candidate.Status = 9
-- ActiveCampaignName   : a Draft / Running / Paused campaign the candidate is Pending in
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaignPickerForIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaignPickerForIRF
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaignPickerForIRF]
    @IRFId  INT,
    @NowIST DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @NowIST = ISNULL(@NowIST, GETDATE());

    SELECT
        c.id                AS CandidateId,
        c.IRFId,
        irf.IRFId           AS IRFName,
        u.FirstName         AS CandidateName,
        u.EmailId           AS CandidateEmail,
        c.Phone             AS CandidatePhone,
        irf.JDTiltle        AS JobTitle,
        cl.Name             AS ClientName,
        c.PlivoCXDisposition,
        CAST(CASE WHEN c.Status = 9 THEN 1 ELSE 0 END AS BIT) AS IsCold,
        CAST(CASE WHEN nextMeeting.id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasMeeting,
        nextMeeting.id      AS UpcomingInterviewId,
        FORMAT(nextMeeting.interviewDate, 'dd MMM yyyy, h:mm tt', 'en-US') AS UpcomingInterviewDate,
        CONVERT(NVARCHAR(50), lastMeeting.interviewDate, 106) AS LastInterviewDate,
        activeCampaign.Name AS ActiveCampaignName
    FROM       dbo.Candidate        c
    INNER JOIN dbo.IRF              irf ON irf.id = c.IRFId
    INNER JOIN dbo.Client           cl  ON cl.id  = irf.ClientId
    INNER JOIN dbo.CandidatesMaster cm  ON cm.id  = c.CandidateMasterId
    INNER JOIN dbo.UserList         u   ON u.id   = cm.UserId
    OUTER APPLY (
        SELECT TOP (1) im.id, im.interviewDate
        FROM dbo.InterviewMeetings im
        WHERE im.candidateEmail = u.EmailId
          AND im.IRFId = irf.IRFId
          AND ISNULL(im.IsDeleted, 0) = 0
          AND ISNULL(im.confirmationStatus, '') <> '2'
          AND im.interviewDate > @NowIST
        ORDER BY im.interviewDate ASC
    ) nextMeeting
    OUTER APPLY (
        SELECT TOP (1) im.interviewDate
        FROM dbo.InterviewMeetings im
        WHERE im.candidateEmail = u.EmailId
          AND im.IRFId = irf.IRFId
          AND im.interviewDate <= @NowIST
        ORDER BY im.interviewDate DESC
    ) lastMeeting
    OUTER APPLY (
        SELECT TOP (1) pc.Name
        FROM dbo.PlivoCXCampaignCandidate pcc
        INNER JOIN dbo.PlivoCXCampaign pc ON pc.Id = pcc.CampaignId
        WHERE pcc.CandidateId = c.id
          AND pcc.[State] = 'Pending'
          AND pc.[Status] IN ('Draft', 'Running', 'Paused')
        ORDER BY pc.Id DESC
    ) activeCampaign
    WHERE c.IRFId = @IRFId
      AND u.EmailId IS NOT NULL
    ORDER BY c.id DESC;
END
GO

-- ---------------------------------------------------------------------------
-- StoredProcedures/usp_GetPlivoCXCampaignNotScheduled.sql
-- ---------------------------------------------------------------------------
-- =============================================
-- PlivoCX campaigns: the "Not-scheduled" candidate source on Create campaign.
--
-- The same list as /Client/NotScheduledMeetings for an internal user
-- (usp_GetClientNotScheduledMeetings with @Clientid = 0), written for speed: that procedure
-- UNIONs every candidate with every meeting across all channels and groups the lot to find the
-- candidates with no meeting (HAVING COUNT(*) = 1), which takes minutes. Here the same rule is a
-- NOT EXISTS per candidate.
--
-- A candidate is on the list when:
--   - their skill channel is active: IRF.EndDate >= now and not paused
--   - they were added to the channel: a CandidateStatus row with Status = 12
--   - they are not cold (ColdCandidateInfos.IsColdCandidate)
--   - there is no InterviewMeetings row at all for their email on that channel
--
-- Scoping, as in the original: Admin and the other internal profiles (idProfile 1, 31, 32) see
-- every channel; anyone else only channels where they are the channel's Risebird HR or are
-- assigned through TransHR.
--   @EmailId : the signed-in user
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaignNotScheduled'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaignNotScheduled
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaignNotScheduled]
    @EmailId VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Role INT, @UserId INT, @HrId INT;
    SELECT @Role = ul.idProfile, @UserId = ul.id FROM dbo.UserList ul WHERE ul.EmailId = @EmailId;
    SELECT @HrId = hr.id FROM dbo.HR hr WHERE hr.UserId = @UserId;
    DECLARE @AllChannels BIT = CASE WHEN @Role IN (1, 31, 32) THEN 1 ELSE 0 END;

    ;WITH Channels AS (
        SELECT irf.id, irf.IRFId, irf.JDTiltle, cl.Name AS ClientName
        FROM       dbo.IRF    irf
        INNER JOIN dbo.Client cl ON cl.id = irf.ClientId
        WHERE irf.EndDate >= GETDATE()
          AND ISNULL(irf.IsPaused, 0) = 0
          AND (
                @AllChannels = 1
             OR irf.RisebirdHr = @UserId
             OR EXISTS (SELECT 1 FROM dbo.TransHR th WHERE th.IRFId = irf.id AND th.HRId = @HrId)
              )
    )
    SELECT
        c.id                                                                 AS CandidateId,
        ch.id                                                                AS IRFId,
        ch.IRFId                                                             AS IRFName,
        LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS CandidateName,
        ul.EmailId                                                           AS CandidateEmail,
        ul.MobileNumber                                                      AS CandidatePhone,
        ch.JDTiltle                                                          AS JobTitle,
        ch.ClientName
    FROM       Channels              ch
    INNER JOIN dbo.Candidate         c  ON c.IRFId = ch.id
    INNER JOIN dbo.CandidatesMaster  cm ON cm.id   = c.CandidateMasterId
    INNER JOIN dbo.UserList          ul ON ul.id   = cm.UserId
    WHERE ul.EmailId IS NOT NULL
      AND EXISTS (
            SELECT 1 FROM dbo.CandidateStatus cs
            WHERE cs.CandidateId = cm.id AND cs.IRFId = ch.id AND cs.Status = 12)
      AND NOT EXISTS (
            SELECT 1 FROM dbo.ColdCandidateInfos cci
            WHERE cci.FKCandidateId = c.id AND cci.IsColdCandidate = 1)
      AND NOT EXISTS (
            SELECT 1 FROM dbo.InterviewMeetings im
            WHERE im.IRFId = ch.IRFId AND im.candidateEmail = ul.EmailId)
    ORDER BY c.id DESC;
END
GO

-- ---------------------------------------------------------------------------
-- The three ElevenLabs campaign agents (created on ElevenLabs on 28 Oct 2026 by
-- Dashboard/CreateElevenLabsCampaignAgents, before the columns above existed, so their
-- rows were not saved). Check the agent ids against ElevenLabs > Agents before running.
-- Skipped for any purpose that already has an ElevenLabs row.
-- ---------------------------------------------------------------------------
INSERT INTO dbo.PlivoCXAgent (Name, Description, TriggerUrl, Purpose, TargetType, CollectsSlot, Channel, IsActive, Provider, ProviderAgentId)
SELECT v.Name, v.Description, '', v.Purpose, 'Candidate', v.CollectsSlot, 'Voice', 1, 'ElevenLabs', v.ProviderAgentId
FROM (VALUES
    ('Schedule',           'collect availability & book', 'schedule',   CAST(1 AS BIT), 'agent_0901m61hhh80egqvnb7xajcx1p6e'),
    ('Reschedule',         'move an existing meeting',    'reschedule', CAST(1 AS BIT), 'agent_9201m61hj17mfx9vq13ypsq2db15'),
    ('Confirm attendance', 'no slot collection',          'confirm',    CAST(0 AS BIT), 'agent_0201m61hj3jdfenv9ervx15wva7z')
) AS v (Name, Description, Purpose, CollectsSlot, ProviderAgentId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PlivoCXAgent a
    WHERE a.Provider = 'ElevenLabs' AND a.Purpose = v.Purpose);
GO

-- Check: three ElevenLabs rows, plus the existing Plivo ones (Provider = Plivo).
SELECT Id, Name, Provider, Purpose, CollectsSlot, ProviderAgentId, IsActive FROM dbo.PlivoCXAgent ORDER BY Provider, Id;
GO
