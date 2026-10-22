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
