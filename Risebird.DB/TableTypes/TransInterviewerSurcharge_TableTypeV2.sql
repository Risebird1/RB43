/****** Object:  UserDefinedTableType [dbo].[TransInterviewerSurcharge_TableTypeV2]    Script Date: 5/17/2023 4:01:20 AM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransInterviewerSurcharge_TableTypeV2')
BEGIN

CREATE TYPE [dbo].[TransInterviewerSurcharge_TableTypeV2] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NULL,
	[PerSuccessRate] [int] NULL,
	[Surcharge] [int] NULL,
	[IsActive] [bit] NOT NULL,
	[IsAvailable] [bit] NOT NULL,
	[Source][varchar](75) null
)

End
GO


