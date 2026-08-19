IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AICodeInterviewQuestionSet')
BEGIN
CREATE TABLE [dbo].[AICodeInterviewQuestionSet](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IsAIProposed] [bit] NOT NULL CONSTRAINT [DF_AIQS_IsAIProposed] DEFAULT (0),
	[ReviewStatus] [tinyint] NOT NULL CONSTRAINT [DF_AIQS_ReviewStatus] DEFAULT (0),
	[IsDeleted] [bit] NOT NULL CONSTRAINT [DF_AIQS_IsDeleted] DEFAULT (0),
	[ApprovedBy] [varchar](150) NULL,
	[ApprovedOn] [datetime] NULL,
	[AIDraftPayload] [nvarchar](max) NULL,
	[CreatedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NOT NULL CONSTRAINT [DF_AIQS_CreatedOn] DEFAULT (GETUTCDATE()),
 CONSTRAINT [PK_AICodeInterviewQuestionSet] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AIQS_IRF'
      AND object_id = OBJECT_ID('AICodeInterviewQuestionSet')
)
BEGIN
    CREATE INDEX IX_AIQS_IRF ON dbo.AICodeInterviewQuestionSet (IRFId) WHERE IsDeleted = 0
END
GO
