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
