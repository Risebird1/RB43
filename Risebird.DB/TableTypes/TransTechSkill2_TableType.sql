IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransTechSkill2_TableType')
BEGIN
Create TYPE [dbo].[TransTechSkill2_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[LevelId] [int] NULL,
	[SkillId] [int] NULL,
	[Weightage] [int] NULL,
	[Concepts] [varchar](50) NULL,
	[Natureofquestion] [varchar](50) NULL,
	[Timelimit] [int] NULL,
	[isEliminationskill][bit]Null,
	[SkillSet][int]Null
)
End
GO

