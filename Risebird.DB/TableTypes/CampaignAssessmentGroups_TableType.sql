IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CampaignAssessmentGroups_TableType')
BEGIN
CREATE TYPE [dbo].[CampaignAssessmentGroups_TableType] AS TABLE(
	[AssessmentGroupId] [int] NOT NULL
)
END
GO




