IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransTemplateInterviewer_TableType')
BEGIN
CREATE TYPE [dbo].[TransTemplateInterviewer_TableType] AS TABLE(
	[UserId] [int] NOT NULL,
	[IsSelected] [bit] NOT NULL
)
END
GO

