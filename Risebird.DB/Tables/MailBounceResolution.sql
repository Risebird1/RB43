/****** Object:  Table [dbo].[MailBounceResolution]   ******/
-- One row per action taken on a bounced/dropped mail row of the Bounce & Dropped Mail page.
-- A row's current state is its latest MailBounceResolution row (by ActionAt); no row = Open.
-- Status: EmailCorrected | Resent | Resolved | Unreachable
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'MailBounceResolution')
BEGIN

CREATE TABLE [dbo].[MailBounceResolution](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SendgridEventId] [int] NOT NULL,       -- SendgridEventData.id of the bounce row
	[CandidateId] [int] NULL,               -- Candidate.id
	[Status] [varchar](20) NOT NULL,
	[Note] [nvarchar](500) NULL,
	[ActionBy] [varchar](150) NOT NULL,
	[ActionAt] [datetime] NOT NULL CONSTRAINT [DF_MailBounceResolution_ActionAt] DEFAULT (GETDATE()),
 CONSTRAINT [PK_MailBounceResolution] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MailBounceResolution_Event'
      AND object_id = OBJECT_ID(N'dbo.MailBounceResolution'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MailBounceResolution_Event
    ON dbo.MailBounceResolution (SendgridEventId, ActionAt DESC)
    INCLUDE ([Status]);
END
GO
