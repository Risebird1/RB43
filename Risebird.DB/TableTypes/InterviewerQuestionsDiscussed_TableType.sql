/****** Object:  UserDefinedTableType [dbo].[InterviewerQuestionsDiscussed_TableType]    Script Date: 22-01-2021 3.32.39 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'InterviewerQuestionsDiscussed_TableType')
BEGIN
	CREATE TYPE [dbo].[InterviewerQuestionsDiscussed_TableType] AS TABLE(
	[InterviewId] [int] NOT NULL,
	[Question] [varchar](max) NULL,
	[RatingId] [int] NOT NULL,
	[Timing] [int])
END

GO


