-- WhatsApp campaigns: one candidate in a campaign - the per-candidate sending state machine.
--
-- State: Pending (not sent yet, or due for a re-send) -> Sent (Interakt accepted it; waiting for the
-- candidate) -> Converted (booked for schedule / reschedule, confirmed for confirm) | Declined (confirm
-- only: answered No) | NoResponse (every allowed send used, opted out, at the 3-per-IRF limit, or the
-- interview passed) | NoNumber (no WhatsApp number on file) | Failed (Interakt rejected it 3 times in a row).
-- usp_RefreshWhatsAppCampaignOutcomes and usp_AdvanceWhatsAppCampaignResends drive the transitions;
-- Mark... on the detail page overrides them by hand.
--
-- Times (FirstSentAt, LastSentAt, NextSendAt, ConvertedAt, SendLockUntil, CreatedAt) are UTC.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'WhatsAppCampaignCandidate')
BEGIN
CREATE TABLE [dbo].[WhatsAppCampaignCandidate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CampaignId] [int] NOT NULL,
	-- Candidate.id (one row per candidate per IRF).
	[CandidateId] [int] NOT NULL,
	[CandidateName] [nvarchar](200) NULL,
	[CandidateEmail] [nvarchar](200) NULL,
	-- The WhatsApp number on file when the candidate was added (display only; each send looks it up again).
	[Phone] [varchar](30) NULL,

	-- Interview context, per row because a list-sourced campaign spans IRFs.
	[IRFId] [int] NULL,
	[IRFName] [nvarchar](100) NULL,
	[JobTitle] [nvarchar](255) NULL,
	[ClientName] [nvarchar](255) NULL,
	-- InterviewMeetings.id of the meeting to confirm (confirm campaigns only).
	[InterviewId] [int] NULL,
	-- The meeting being moved or confirmed, e.g. "07 Mar 2026".
	[OriginalInterviewDate] [nvarchar](50) NULL,

	-- Pending | Sent | Converted | Declined | NoResponse | NoNumber | Failed
	[State] [varchar](20) NOT NULL CONSTRAINT [DF_WhatsAppCampaignCandidate_State] DEFAULT ('Pending'),
	[Sends] [int] NOT NULL CONSTRAINT [DF_WhatsAppCampaignCandidate_Sends] DEFAULT (0),
	-- Failed Interakt calls in a row; 3 -> Failed.
	[FailedSends] [int] NOT NULL CONSTRAINT [DF_WhatsAppCampaignCandidate_FailedSends] DEFAULT (0),
	[FirstSentAt] [datetime] NULL,
	[LastSentAt] [datetime] NULL,
	-- Earliest time the next send may go (NULL = now).
	[NextSendAt] [datetime] NULL,
	-- The send helper's answer, e.g. "Message sent successfully." or "Skipped: opted out".
	[LastResult] [nvarchar](200) NULL,
	[LastError] [nvarchar](1000) NULL,
	-- Reserved for an Interakt message id once the API returns one.
	[InteraktId] [varchar](100) NULL,
	[ConvertedAt] [datetime] NULL,
	-- Why the row ended where it did: "Booked 12 Nov 2026, 3:00 PM", "Opted out of WhatsApp", "marked", ...
	[OutcomeNote] [nvarchar](300) NULL,
	-- Send lock: set atomically when a row is claimed and cleared once the send is recorded, so Send now,
	-- a per-row Re-send and the scheduler never send to the same candidate at the same time. A lock older
	-- than its expiry (a crash mid-send) no longer blocks.
	[SendLockUntil] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_WhatsAppCampaignCandidate_CreatedAt] DEFAULT (GETUTCDATE()),
	[UpdatedAt] [datetime] NULL,
 CONSTRAINT [PK_WhatsAppCampaignCandidate] PRIMARY KEY CLUSTERED ([Id] ASC),
 CONSTRAINT [UQ_WhatsAppCampaignCandidate_Campaign_Candidate] UNIQUE ([CampaignId], [CandidateId])
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_WhatsAppCampaignCandidate_Campaign_State' AND object_id = OBJECT_ID(N'dbo.WhatsAppCampaignCandidate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_WhatsAppCampaignCandidate_Campaign_State]
	ON [dbo].[WhatsAppCampaignCandidate] ([CampaignId], [State])
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_WhatsAppCampaignCandidate_Candidate_State' AND object_id = OBJECT_ID(N'dbo.WhatsAppCampaignCandidate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_WhatsAppCampaignCandidate_Candidate_State]
	ON [dbo].[WhatsAppCampaignCandidate] ([CandidateId], [State]) INCLUDE ([CampaignId])
END
GO
