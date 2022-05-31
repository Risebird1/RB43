IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'MCQQuestions_TableType')
BEGIN

CREATE TYPE [dbo].[MCQQuestions_TableType] AS TABLE(
	[PartitionKey] [varchar](50) NULL,
	[QuestionType] [varchar](50) NULL,
	[Question] [varchar](max) NULL,
	[Option1] [varchar](1000) NULL,
	[Option2] [varchar](1000) NULL,
	[Option3] [varchar](1000) NULL,
	[Option4] [varchar](1000) NULL,
	[Answer] [varchar](10) NULL,
	[Explanation] [varchar](100) NULL,
	[MCQId] [varchar](50) NULL,
	[QuestionNumber] [int] NULL
)
END
GO
