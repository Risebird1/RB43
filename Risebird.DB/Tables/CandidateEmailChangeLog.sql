/****** Object:  Table [dbo].[CandidateEmailChangeLog]   ******/
-- Audit of candidate email corrections. Also lets usp_GetEmailRecipientsActivityBounceDropped keep
-- showing a bounce row after its address was fixed (SendgridEventData.email = OldEmail).
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'CandidateEmailChangeLog')
BEGIN

CREATE TABLE [dbo].[CandidateEmailChangeLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,                -- UserList.id
	[CandidateId] [int] NULL,
	[OldEmail] [varchar](150) NOT NULL,
	[NewEmail] [varchar](150) NOT NULL,
	[Source] [varchar](50) NOT NULL,        -- 'BounceFixEmail'
	[ChangedBy] [varchar](150) NOT NULL,
	[ChangedAt] [datetime] NOT NULL CONSTRAINT [DF_CandidateEmailChangeLog_ChangedAt] DEFAULT (GETDATE()),
 CONSTRAINT [PK_CandidateEmailChangeLog] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CandidateEmailChangeLog_Old'
      AND object_id = OBJECT_ID(N'dbo.CandidateEmailChangeLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CandidateEmailChangeLog_Old
    ON dbo.CandidateEmailChangeLog (OldEmail, ChangedAt DESC)
    INCLUDE (UserId);
END
GO
