/****** Object:  UserDefinedTableType [dbo].[TransSMEVerifiedInterviewer_TableType]  ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransSMEVerifiedInterviewer_TableType')
BEGIN

CREATE TYPE [dbo].[TransSMEVerifiedInterviewer_TableType] AS TABLE(
	[id] [int] NOT NULL,
	[IsSMEVerified] [bit] NULL
)

End
GO


