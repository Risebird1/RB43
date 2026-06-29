-- Migration: per-attempt batch-call tracking.
--
-- Each manual/auto batch call against a campaign is now an "attempt". An attempt is identified by a
-- sequential AttemptNumber (per campaign) and a human-readable AttemptName
-- ("{CampaignName}_Attempt{N}_{yyyy-MM-dd_HH:mm}"). Multiple attempts per campaign are expected, so
-- these columns let the Campaigns page show how many times a batch call was run and how each fared.

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AttemptNumber' AND Object_ID = Object_ID(N'IABatchCallLog'))
BEGIN
    ALTER TABLE IABatchCallLog ADD [AttemptNumber] INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AttemptName' AND Object_ID = Object_ID(N'IABatchCallLog'))
BEGIN
    ALTER TABLE IABatchCallLog ADD [AttemptName] NVARCHAR(300) NULL;
END
GO
