/****** Object:  UserDefinedTableType [dbo].[TechSkillReport_TableType]    Script Date: 25-06-2020 1.16.54 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TechSkillReport_TableType')
BEGIN

CREATE TYPE [dbo].[TechSkillReport_TableType] AS TABLE(
	[InterviewId] [int] NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[SkillId] [int] NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[TechComments] [varchar](500) 
)


END
GO


