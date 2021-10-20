IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'EventTimeSlots_TableType')
BEGIN
CREATE TYPE [dbo].[EventTimeSlots_TableType] AS TABLE(
	[Daytype] [varchar](15) NOT NULL,
	[Start] [varchar](10) NOT NULL,
	[End] [varchar](10) NOT NULL,
	[dayCheck] [bit] NOT NULL,
	[Eventid] [int] NOT NULL
)
END
GO


