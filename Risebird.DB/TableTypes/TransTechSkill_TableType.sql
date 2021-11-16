/****** Object:  UserDefinedTableType [dbo].[TransTechSkill_TableType]    Script Date: 24-06-2020 1.45.06 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransTechSkill_TableType')
BEGIN

CREATE TYPE [dbo].[TransTechSkill_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[LevelId] [int] NULL,
	[SkillId] [int] NULL,
	[Weightage] [int] NULL,
	[Concepts] [varchar](50) NULL,
	[Natureofquestion] [varchar](50) NULL,
	[Timelimit] [int] NULL
)

End
GO
