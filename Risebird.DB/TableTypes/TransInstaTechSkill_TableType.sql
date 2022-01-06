/****** Object:  UserDefinedTableType [dbo].[TransTechSkill_TableType]    Script Date: 20-08-2021 14:41:19 ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransInstaTechSkill_TableType')
BEGIN
CREATE TYPE [dbo].[TransInstaTechSkill_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[LevelId] [int] NULL,
	[SkillId] [varchar](50) NULL,
	[ActualLevelId] [int] NULL
)
END
GO


