/****** Object:  UserDefinedTableType [dbo].[TransSkillGeneral_TableType]    Script Date: 24-06-2020 1.43.56 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransSkillGeneral_TableType')
BEGIN

CREATE TYPE [dbo].[TransSkillGeneral_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[LevelId] [int] NULL,
	[Type] [varchar](50) NULL,
	[Value] [varchar](max) NULL,
	[Weightage] [int] NULL
)

End
GO


