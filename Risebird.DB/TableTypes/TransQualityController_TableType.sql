/****** Object:  UserDefinedTableType [dbo].[TransQualityController_TableType]    Script Date: 24-06-2020 2.19.47 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransQualityController_TableType')
BEGIN

CREATE TYPE [dbo].[TransQualityController_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NULL,
	[PerSuccessRate] [int] NULL,
	[IsActive] [bit] NOT NULL
)

END
GO


