/****** Object:  UserDefinedTableType [dbo].[TransVMTriggerDetails_TableType]    Script Date: 21-09-2020 12.03.51 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransVMTriggerDetails_TableType')
BEGIN

CREATE TYPE [dbo].[TransVMTriggerDetails_TableType] AS TABLE(
	[VMPropertyId] [int] NOT NULL,
	[LastAction] [varchar](100) NOT NULL,
	[ExpectedStatus] [varchar](100) NOT NULL
)

End
GO


