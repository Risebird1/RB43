/****** Object:  UserDefinedTableType [dbo].[TransHR_TableType]    Script Date: 24-06-2020 2.20.06 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransHR_TableType')
BEGIN

CREATE TYPE [dbo].[TransHR_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NOT NULL
)

END
GO


