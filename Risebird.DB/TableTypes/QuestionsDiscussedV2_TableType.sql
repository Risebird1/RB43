IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'QuestionsDiscussedV2_TableType')
BEGIN

CREATE TYPE [dbo].[QuestionsDiscussedV2_TableType] AS TABLE(
	[InterviewId] [int] NOT NULL,
	[Comment] [varchar](max) NULL,
	[RatingId] [int] NOT NULL,
	[Timing] [int] NULL,
	[SkillName] [varchar](max) NULL,
	[SkillSet] [int] null
)

End
GO