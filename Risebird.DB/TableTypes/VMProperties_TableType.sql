/****** Object:  UserDefinedTableType [dbo].[VMProperties_TableType]    Script Date: 21-09-2020 12.03.22 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'VMProperties_TableType')
BEGIN

CREATE TYPE [dbo].[VMProperties_TableType] AS TABLE(
	[Name] [varchar](150) NOT NULL,
	[ResourceGroupName] [varchar](150) NOT NULL,
	[Status] [int] NULL,
	[IsAdded] [bit] NOT NULL,
	[IsRemoved] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[shutdowntime] [int] NULL,
	[deletetime] [int] NULL,
	[VmLocation] [varchar](150) NULL
)

End
GO


