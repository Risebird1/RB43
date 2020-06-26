/****** Object:  UserDefinedTableType [dbo].[TransRecruiter_TableType]    Script Date: 24-06-2020 2.20.22 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransRecruiter_TableType')
BEGIN

CREATE TYPE [dbo].[TransRecruiter_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NOT NULL
)

END
GO


