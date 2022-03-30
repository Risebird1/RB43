IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'ClonableTemplateQuestions_TableType')
BEGIN

CREATE TYPE [dbo].[ClonableTemplateQuestions_TableType] AS TABLE(
	[Skill] [int] NULL,
	[Question] [varchar](max) NULL,
	[UserId][int] NULL
)

End
GO



