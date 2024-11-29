IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV2')
BEGIN
CREATE TYPE [dbo].[AddIRFSampleQuestions_TableTypeV2] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](max) NULL,
	[CreatedBy] [varchar](150) NULL,
	[SkillName] [varchar](500) NULL
)
END
GO