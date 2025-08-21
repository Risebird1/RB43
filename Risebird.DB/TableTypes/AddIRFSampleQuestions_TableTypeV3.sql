IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV3')
BEGIN
CREATE TYPE [dbo].[AddIRFSampleQuestions_TableTypeV3] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](max) NULL,
	[CreatedBy] [varchar](150) NULL,
	[SkillName] [varchar](500) NULL,
	[ExpectedAnswer] [varchar](5000) NULL
)
END
GO