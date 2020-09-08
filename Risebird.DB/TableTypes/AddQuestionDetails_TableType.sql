IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddQuestionDetails_TableType')
BEGIN
CREATE TYPE [dbo].[AddQuestionDetails_TableType] AS TABLE(
	[id] [int] NULL,
	[Interviewid] [int] NOT NULL,
	[Question] [varchar](max) NULL,
	[Priority] [int] NOT NULL,
	[VideoTitle] [varchar](100) NULL,
	[VideoLink] [varchar](max) NULL
)
END
GO




