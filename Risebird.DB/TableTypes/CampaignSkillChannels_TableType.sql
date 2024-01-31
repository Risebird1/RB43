IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CampaignSkillChannels_TableType')
BEGIN
CREATE TYPE [dbo].[CampaignSkillChannels_TableType] AS TABLE(
	[IRFId] [int] NOT NULL
)
END
GO




