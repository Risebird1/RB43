IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AIQuestionSets')
BEGIN
CREATE TABLE [dbo].[AIQuestionSets](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[SetName] [varchar](10) NOT NULL,
	[TotalMinutes] [int] NULL,
	[TotalPoints] [int] NULL,
	[PromptId] [varchar](200) NULL,
	[PromptVersion] [varchar](50) NULL,
	[CreatedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL,
 CONSTRAINT [PK_AIQuestionSets] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- A channel cannot hold two papers with the same letter. The set name is derived server-side
-- from the set index, so a duplicate here means a save path bypassed BuildSetName.
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_AIQuestionSets_IRFId_SetName'
    AND object_id = OBJECT_ID(N'dbo.AIQuestionSets'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_AIQuestionSets_IRFId_SetName]
    ON [dbo].[AIQuestionSets] ([IRFId] ASC, [SetName] ASC)
END
GO
