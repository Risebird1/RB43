/****** Object:  UserDefinedTableType [dbo].[TransInterviewer_TableType]    Script Date: 24-06-2020 1.45.15 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransInterviewer_TableType')
BEGIN

CREATE TYPE [dbo].[TransInterviewer_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NULL,
	[PerSuccessRate] [int] NULL,
	[IsActive] [bit] not null,
	[IsAvailable] [bit] not null
)

End
GO


