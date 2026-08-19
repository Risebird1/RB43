IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddAIQuestionAnswers_TableType')
BEGIN
CREATE TYPE [dbo].[AddAIQuestionAnswers_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](max) NULL,
	[CreatedBy] [varchar](150) NULL,
	[SkillName] [nvarchar](500) NULL,
	[IdealAnswer] [varchar](max) NULL
)
END
GO
