IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TechSkillComments_TableType')
BEGIN
CREATE TYPE [dbo].[TechSkillComments_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](max) NULL,
	[UserId] [int] NULL
)
END
GO


