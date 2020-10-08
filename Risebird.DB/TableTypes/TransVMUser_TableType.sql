/****** Object:  UserDefinedTableType [dbo].[TransVMUser_TableType]    Script Date: 25-09-2020 11.55.12 AM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransVMUser_TableType')
BEGIN

CREATE TYPE [dbo].[TransVMUser_TableType] AS TABLE(
	[VmId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[ProfileId] [int] NULL,
	[SharedOn] [datetime] NULL

)

END
GO


