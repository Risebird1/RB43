-- SortOrder lives only here, never in AIQuestionSetItems. usp_SaveAIQuestionSets inserts
-- ORDER BY SortOrder so that the table's identity order becomes the ask order.
-- SQL Server cannot alter a table type in place: if this shape must change, add a new
-- versioned type (AIQuestionSetItems_TableTypeV2) instead of editing this one.
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AIQuestionSetItems_TableType')
BEGIN
CREATE TYPE [dbo].[AIQuestionSetItems_TableType] AS TABLE(
	[SetName] [varchar](10) NOT NULL,
	[QuestionId] [int] NOT NULL,
	[SkillName] [nvarchar](500) NULL,
	[TotalMinutes] [int] NULL,
	[TotalPoints] [int] NULL,
	[SortOrder] [int] NOT NULL
)
END
GO
