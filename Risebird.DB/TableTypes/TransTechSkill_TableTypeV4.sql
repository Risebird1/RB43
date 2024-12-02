IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransTechSkill_TableTypeV4')
BEGIN
Create TYPE [dbo].[TransTechSkill_TableTypeV4] AS TABLE(
	[IRFId] [int] NOT NULL,
	[LevelId] [int] NULL,
	[SkillId] [int] NULL,
	[Weightage] [int] NULL,
	[Concepts] [varchar](200) NULL,
	[Natureofquestion] [varchar](200) NULL,
	[Timelimit] [int] NULL,
	[isEliminationskill][bit]Null,
	[SkillSet][int]Null,
	[SkillName][varchar](500) Null
)
End
GO

