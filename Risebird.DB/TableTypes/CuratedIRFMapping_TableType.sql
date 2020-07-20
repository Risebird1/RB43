/****** Object:  UserDefinedTableType [dbo].[CuratedIRFMapping_TableType]    Script Date: 13-07-2020 8.22.47 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CuratedIRFMapping_TableType')
BEGIN

CREATE TYPE [dbo].[CuratedIRFMapping_TableType] AS TABLE(
	[CuratedGroupId] [int] NOT NULL,
	[IRFId] [int] NOT NULL,
	[IsAdded] [bit] NOT NULL
)

End
GO


