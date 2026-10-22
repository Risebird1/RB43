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
