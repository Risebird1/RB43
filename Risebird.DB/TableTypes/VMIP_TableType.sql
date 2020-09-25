/****** Object:  UserDefinedTableType [dbo].[VMProperties_TableType]    Script Date: 22-09-2020 12.39.10 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'VMIP_TableType')
BEGIN

CREATE TYPE [dbo].[VMIP_TableType] AS TABLE(
	[id] [int] NOT NULL,
	[OSType] [varchar](25) NULL,
	[PublicIP] [varchar](100) NULL,
	[PrivateIP] [varchar](100) NULL
)

End
GO


