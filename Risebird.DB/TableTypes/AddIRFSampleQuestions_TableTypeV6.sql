IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV6')
BEGIN
CREATE TYPE [dbo].[AddIRFSampleQuestions_TableTypeV6] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [nvarchar](max) NULL,
	[CreatedBy] [varchar](150) NULL,
	[SkillName] [nvarchar](500) NULL,
	[ExpectedAnswer] [nvarchar](max) NULL,
	[IsCodingQuestion] [bit] NULL,
	[CodingTimeLimitInMinutes] [int] NULL,
	[QuestionGist] [nvarchar](max) NULL
)
END
GO
