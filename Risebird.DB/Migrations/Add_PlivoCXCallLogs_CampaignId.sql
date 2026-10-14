-- PlivoCX campaigns: ties every call log to the campaign that dialled it (NULL for timer-job
-- and one-off calls). The application writes it with a separate UPDATE after the INSERT, so
-- calls keep logging on a database where this script has not run yet.
IF COL_LENGTH('dbo.PlivoCXCallLogs', 'CampaignId') IS NULL
BEGIN
	ALTER TABLE [dbo].[PlivoCXCallLogs] ADD [CampaignId] [int] NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PlivoCXCallLogs_CampaignId' AND object_id = OBJECT_ID(N'dbo.PlivoCXCallLogs'))
BEGIN
	CREATE NONCLUSTERED INDEX [IX_PlivoCXCallLogs_CampaignId]
		ON [dbo].[PlivoCXCallLogs] ([CampaignId], [CandidateId])
		INCLUDE ([CallStatus], [CreatedAt])
		WHERE [CampaignId] IS NOT NULL;
END
GO
