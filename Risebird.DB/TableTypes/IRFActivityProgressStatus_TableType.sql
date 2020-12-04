/****** Object:  UserDefinedTableType [dbo].[IRFActivityProgressStatus_TableType]    Script Date: 04-12-2020 23:42:18 ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'IRFActivityProgressStatus_TableType')
BEGIN

CREATE TYPE [dbo].[IRFActivityProgressStatus_TableType] AS TABLE(
	[id] [int] NOT NULL,
	[Comment] [varchar](250) NULL,
	[idProgress] [int] NOT NULL,
	[Userid] [varchar](200) NULL,
	[Status] [varchar](50) NULL
)
GO


