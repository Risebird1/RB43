/****** Object:  UserDefinedTableType [dbo].[QuestionsDiscussed_TableType]    Script Date: 25-06-2020 1.17.12 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'QuestionsDiscussed_TableType')
BEGIN

CREATE TYPE [dbo].[QuestionsDiscussed_TableType] AS TABLE(
	[InterviewId] [int] NOT NULL,
	[Comment] [varchar](max) NULL,
	[RatingId] [int] NOT NULL,
	[Timing] [int] NULL,
	[SkillName] [varchar](max) NULL
)

End
GO


