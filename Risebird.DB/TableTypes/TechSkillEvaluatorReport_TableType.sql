/****** Object:  UserDefinedTableType [dbo].[TechSkillEvaluatorReport_TableType]    Script Date: 25-06-2020 7.42.05 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TechSkillEvaluatorReport_TableType')
BEGIN

CREATE TYPE [dbo].[TechSkillEvaluatorReport_TableType] AS TABLE(
	[RequiredLevelId] [int] NOT NULL,
	[SkillId] [int] NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[CandidateId] [int] NULL,
	[IRFId] [int] NULL
)

END
GO


