IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableType')
BEGIN
CREATE TYPE [dbo].[AddIRFSampleQuestions_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](max) NULL,
	[CreatedBy] [varchar](150) NULL
)
END
GO