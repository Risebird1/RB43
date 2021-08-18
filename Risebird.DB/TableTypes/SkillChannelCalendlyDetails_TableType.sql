IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'SkillChannelCalendlyDetails_TableType')
BEGIN
CREATE TYPE [dbo].[SkillChannelCalendlyDetails_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[ResourceUri] [varchar](1000) NULL
)
END
GO

