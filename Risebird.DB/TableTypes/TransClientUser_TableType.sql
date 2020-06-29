/****** Object:  UserDefinedTableType [dbo].[TransClientUser_TableType]    Script Date: 24-06-2020 2.19.26 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransClientUser_TableType')
BEGIN

CREATE TYPE [dbo].[TransClientUser_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[ClientUserId] [int] NOT NULL
)

END
GO


