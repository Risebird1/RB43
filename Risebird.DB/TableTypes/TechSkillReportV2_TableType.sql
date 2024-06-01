
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TechSkillReportV2_TableType')
BEGIN

CREATE TYPE [dbo].[TechSkillReportV2_TableType] AS TABLE(
	[InterviewId] [int] NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[SkillId] [int] NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[TechComments] [varchar](500),
	[SkillSet][int] Null
)
END
GO