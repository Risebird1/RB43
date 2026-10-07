/****** Object:  Table [dbo].[InterviewInviteSequence]   ******/
-- Bounce & Dropped – QA round 1 (Q4): calendar SEQUENCE of each interview's invite.
-- Every interview .ics now uses the stable UID interview-{id}@risebird.io; SEQUENCE must grow with every invite,
-- reschedule, bounce resend or cancel so calendars update (or cancel) the one existing event instead of adding
-- a second one. Incremented by usp_NextInterviewInviteSequence; no row = 0.
--
-- Kept in its own table rather than as an InterviewMeetings column on purpose: every UPDATE of InterviewMeetings
-- fires tr_InterviewMeetings_ForUpdate (history row, wallet billing for redo interviews, cancellation webhooks),
-- which must not run just because an invite was mailed. A new small table is also instant to create, unlike an
-- ALTER on InterviewMeetings.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'InterviewInviteSequence')
BEGIN

CREATE TABLE [dbo].[InterviewInviteSequence](
	[InterviewId] [int] NOT NULL,           -- InterviewMeetings.id
	[Sequence] [int] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL CONSTRAINT [DF_InterviewInviteSequence_UpdatedAt] DEFAULT (GETDATE()),
 CONSTRAINT [PK_InterviewInviteSequence] PRIMARY KEY CLUSTERED
(
	[InterviewId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO
