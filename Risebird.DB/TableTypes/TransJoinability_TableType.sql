/****** Object:  UserDefinedTableType [dbo].[TransJoinability_TableType]    Script Date: 24-06-2020 1.43.36 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransJoinability_TableType')
BEGIN

CREATE TYPE [dbo].[TransJoinability_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[LevelId] [int] NULL,
	[Type] [nvarchar](50) NULL,
	[Value] [varchar](max) NULL,
	[Weightage] [int] NULL
)

END
GO


