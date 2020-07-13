/****** Object:  UserDefinedTableType [dbo].[CuratedGroupUsers_TableType]    Script Date: 13-07-2020 8.22.39 PM ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CuratedGroupUsers_TableType')
BEGIN

CREATE TYPE [dbo].[CuratedGroupUsers_TableType] AS TABLE(
	[UserId] [int] NOT NULL,
	[CuratedGroupId] [int] NOT NULL,
	[IsAdded] [bit] NOT NULL
)

End
GO


