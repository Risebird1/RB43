/****** Object:  UserDefinedTableType [dbo].[TransInterviewer_TableTypeV2]    Script Date: 24-06-2020 1.45.15 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransInterviewer_TableTypeV2')
BEGIN

CREATE TYPE [dbo].[TransInterviewer_TableTypeV2] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NULL,
	[PerSuccessRate] [int] NULL,
	[IsActive] [bit] not null,
	[IsAvailable] [bit] not null,
	[Source] [varchar](75) null
)

End
GO


