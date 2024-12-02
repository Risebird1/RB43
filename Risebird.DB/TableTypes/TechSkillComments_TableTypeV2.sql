IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TechSkillComments_TableTypeV2')
BEGIN
CREATE TYPE [dbo].[TechSkillComments_TableTypeV2] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](max) NULL,
	[UserId] [int] NULL,
	[SkillName] [varchar](500) NULL
)
END
GO


