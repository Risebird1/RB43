-- Migration: Add BatchCallTriggeredAt to AGCampaigns for tracking campaign-initiated batch calls

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'BatchCallTriggeredAt' AND Object_ID = Object_ID(N'AGCampaigns'))
BEGIN
    ALTER TABLE AGCampaigns ADD [BatchCallTriggeredAt] DATETIME NULL
END
GO

-- Backfill: mark all existing campaigns as already triggered so they are not
-- re-processed when the CampaignBatchCallJob is re-enabled in the future.
UPDATE AGCampaigns
SET BatchCallTriggeredAt = GETDATE()
WHERE BatchCallTriggeredAt IS NULL;
GO
