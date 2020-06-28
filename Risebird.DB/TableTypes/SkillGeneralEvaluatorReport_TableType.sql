/****** Object:  UserDefinedTableType [dbo].[SkillGeneralEvaluatorReport_TableType]    Script Date: 25-06-2020 7.42.14 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'SkillGeneralEvaluatorReport_TableType')
BEGIN

CREATE TYPE [dbo].[SkillGeneralEvaluatorReport_TableType] AS TABLE(
	[RequiredLevelId] [int] NOT NULL,
	[Type] [nvarchar](200) NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[Value] [varchar](500) NULL,
	[CandidateId] [int] NULL,
	[IRFId] [int] NULL
)

END
GO


