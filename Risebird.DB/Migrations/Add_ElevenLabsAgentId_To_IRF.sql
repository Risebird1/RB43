-- Maps a skill channel (IRF) to an ElevenLabs agent for the WhatsApp phone screener.
-- Idempotent: safe to run multiple times.
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'ElevenLabsAgentId'
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    PRINT 'Adding ElevenLabsAgentId column to IRF table...';
    ALTER TABLE IRF
    ADD ElevenLabsAgentId VARCHAR(200) NULL;
END
ELSE
BEGIN
    PRINT 'ElevenLabsAgentId column already exists on IRF table. Skipping.';
END
GO
