/****** Object:  UserDefinedTableType [dbo].[CommonId_TableType]  ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CommonId_TableType')
BEGIN

CREATE TYPE [dbo].[CommonId_TableType] AS TABLE(
	[id] [int] NOT NULL
)

END
GO


