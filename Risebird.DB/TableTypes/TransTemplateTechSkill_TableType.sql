IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransTemplateTechSkill_TableType')
BEGIN
CREATE TYPE [dbo].[TransTemplateTechSkill_TableType] AS TABLE(
	[SkillId] [int] NULL,
	[LevelId] [int] NULL,
	[Weightage] [int] NULL,
	[IsSelected] [bit] NOT NULL
)
END
GO

