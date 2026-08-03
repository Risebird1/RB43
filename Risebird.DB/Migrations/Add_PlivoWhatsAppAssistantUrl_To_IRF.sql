-- =============================================
-- Migration: Add PlivoWhatsAppAssistantUrl column to IRF table
-- Date: 2026-08-03
-- Description: Maps each skill channel (IRF) to a Plivo CX conversational WhatsApp
--              agent (separate from the voice agent stored in PlivoAssistantUrl).
--              In Plivo CX an "assistant" is addressed by its flow trigger URL, so
--              this column stores that full trigger endpoint URL. The Phone Screener
--              feature reads this URL to start WhatsApp pre-screening conversations
--              with candidates added to the skill channel.
-- =============================================

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'PlivoWhatsAppAssistantUrl'
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    PRINT 'Adding PlivoWhatsAppAssistantUrl column to IRF table...';
    ALTER TABLE IRF
    ADD PlivoWhatsAppAssistantUrl VARCHAR(1000) NULL;
END
ELSE
BEGIN
    PRINT 'PlivoWhatsAppAssistantUrl column already exists on IRF table. Skipping.';
END
GO
