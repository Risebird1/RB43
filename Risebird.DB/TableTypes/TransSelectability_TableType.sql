/****** Object:  UserDefinedTableType [dbo].[TransSelectability_TableType]    Script Date: 24-06-2020 1.43.47 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransSelectability_TableType')
BEGIN

CREATE TYPE [dbo].[TransSelectability_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[LevelId] [int] NOT NULL,
	[Type] [varchar](200) NULL,
	[Value] [varchar](max) NULL,
	[Weightage] [int] NULL
)

End
GO


