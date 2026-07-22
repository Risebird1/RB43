-- =============================================
-- Migration: Add PlivoAssistantUrl column to IRF table
-- Date: 2026-07-21
-- Description: Maps each skill channel (IRF) to a Plivo CX conversational voice
--              agent. In Plivo CX an "assistant" is addressed by its flow trigger
--              URL, so this column stores that full trigger endpoint URL. The
--              Phone Screener feature reads this URL to place pre-screening calls
--              to candidates added to the skill channel.
-- =============================================

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'PlivoAssistantUrl'
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    PRINT 'Adding PlivoAssistantUrl column to IRF table...';
    ALTER TABLE IRF
    ADD PlivoAssistantUrl VARCHAR(1000) NULL;
END
ELSE
BEGIN
    PRINT 'PlivoAssistantUrl column already exists on IRF table. Skipping.';
END
GO
