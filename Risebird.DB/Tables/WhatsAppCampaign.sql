-- WhatsApp campaigns: one operator-run campaign that sends one approved Interakt template (C1 schedule
-- invite, C2 reschedule invite or C3 confirmation reminder) to a chosen list of candidates, re-sends on a
-- schedule, and tracks who booked or confirmed. A copy of the PlivoCX campaign feature with the calls
-- replaced by WhatsApp sends.
--
-- Status: Draft -> Running (Start automatic) <-> Paused -> Completed. Draft and Paused campaigns are only
-- sent by Send now / per-row Re-send; the WhatsAppCampaignScheduler timer sends Running ones inside the
-- policy window and marks them Completed when nobody is left Pending or Sent, or EndsOn has passed.
--
-- A campaign is sourced from one IRF (IRFId set) or from a centralized list that can span many IRFs
-- (IRFId NULL, IRFName = "Not-scheduled list" etc.); each candidate row keeps its own IRF.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'WhatsAppCampaign')
BEGIN
CREATE TABLE [dbo].[WhatsAppCampaign](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	-- IRF | NotScheduled | Reschedule | Unconfirmed
	[SourceType] [varchar](20) NOT NULL CONSTRAINT [DF_WhatsAppCampaign_SourceType] DEFAULT ('IRF'),
	-- schedule (C1) | reschedule (C2) | confirm (C3)
	[Purpose] [varchar](20) NOT NULL,
	-- The Interakt event the template is sent on: Schedule Interview | Reschedule Interview | Interview Reminder
	[EventName] [varchar](100) NOT NULL,
	-- IRF.id when sourced By IRF.
	[IRFId] [int] NULL,
	-- IRF.IRFId business string, or the list name ("Not-scheduled list", "Reschedule list", ...).
	[IRFName] [nvarchar](100) NULL,
	-- Client name, or a summary such as "12 clients" for a list-sourced campaign.
	[ClientName] [nvarchar](200) NULL,
	-- Draft | Running | Paused | Completed
	[Status] [varchar](20) NOT NULL CONSTRAINT [DF_WhatsAppCampaign_Status] DEFAULT ('Draft'),

	-- Sending policy
	[WindowOpens] [time](0) NOT NULL CONSTRAINT [DF_WhatsAppCampaign_WindowOpens] DEFAULT ('09:00'),
	[WindowCloses] [time](0) NOT NULL CONSTRAINT [DF_WhatsAppCampaign_WindowCloses] DEFAULT ('20:00'),
	-- IANA or Windows time zone id.
	[TimeZone] [varchar](60) NOT NULL CONSTRAINT [DF_WhatsAppCampaign_TimeZone] DEFAULT ('Asia/Kolkata'),
	-- Sends per candidate in this campaign, 1-3; the 3-per-IRF limit of C1 / C2 still applies on top.
	[MaxSends] [int] NOT NULL CONSTRAINT [DF_WhatsAppCampaign_MaxSends] DEFAULT (2),
	-- A Sent candidate who has not booked / confirmed is sent again after this many hours.
	[ResendAfterHours] [int] NOT NULL CONSTRAINT [DF_WhatsAppCampaign_ResendAfterHours] DEFAULT (24),
	-- Candidates claimed per scheduler pass (every 5 minutes) or per Send now.
	[BatchSize] [int] NOT NULL CONSTRAINT [DF_WhatsAppCampaign_BatchSize] DEFAULT (50),
	[StartsOn] [date] NULL,
	[EndsOn] [date] NULL,

	[CreatedBy] [varchar](150) NULL,
	[CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_WhatsAppCampaign_CreatedAt] DEFAULT (GETUTCDATE()),
	[UpdatedAt] [datetime] NULL,
 CONSTRAINT [PK_WhatsAppCampaign] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_WhatsAppCampaign_Status' AND object_id = OBJECT_ID(N'dbo.WhatsAppCampaign'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_WhatsAppCampaign_Status] ON [dbo].[WhatsAppCampaign] ([Status])
END
GO
