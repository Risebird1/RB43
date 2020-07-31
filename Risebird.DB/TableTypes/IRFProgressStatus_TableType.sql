/****** Object:  UserDefinedTableType [dbo].[IRFProgressStatus_TableType]    Script Date: 29-07-2020 10.55.01 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'IRFProgressStatus_TableType')
BEGIN

CREATE TYPE [dbo].[IRFProgressStatus_TableType] AS TABLE(
	[id] [int] NOT NULL,
	[Comment] [varchar](250) NULL,
	[idProgress] [int] NOT NULL
)
END
GO


