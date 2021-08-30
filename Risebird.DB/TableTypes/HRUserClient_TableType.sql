IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'HRUserClient_TableType')
BEGIN

CREATE TYPE [dbo].[HRUserClient_TableType] AS TABLE(
	[Userid] [int] NOT NULL,
	[Clientid] [int] NOT NULL
)

END
GO