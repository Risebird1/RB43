IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'RecommendedSlots_TableType')
BEGIN
CREATE TYPE [dbo].[RecommendedSlots_TableType] AS TABLE(
	[SlotTime] [datetime] NOT NULL
)
END
GO