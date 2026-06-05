-- Migration: Add CampaignId to IABatchCallLog to trace batch calls back to their source campaign

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'CampaignId' AND Object_ID = Object_ID(N'IABatchCallLog'))
BEGIN
    ALTER TABLE IABatchCallLog ADD [CampaignId] INT NULL
END
GO
