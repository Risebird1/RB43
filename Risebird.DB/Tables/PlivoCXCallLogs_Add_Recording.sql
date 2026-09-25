-- =============================================================================
-- PlivoCXCallLogs — add call recording capture
--
-- Adds:
--   PlivoCallUuid  — Plivo voice call UUID, taken from the Plivo CX hangup event
--   RecordingUrl   — recording URL pulled from Plivo's Recording API by call UUID
--
-- Both columns are nullable, so existing rows and existing code are unaffected.
-- Idempotent: safe to run more than once.
--
-- Run BEFORE redeploying usp_GetPlivoCXCallLogDetails.sql (that procedure selects
-- pcl.RecordingUrl).
-- =============================================================================

IF COL_LENGTH('dbo.PlivoCXCallLogs', 'PlivoCallUuid') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlivoCXCallLogs] ADD [PlivoCallUuid] NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.PlivoCXCallLogs', 'RecordingUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlivoCXCallLogs] ADD [RecordingUrl] NVARCHAR(1000) NULL;
END
GO

-- Supports the recording backfill query (UUID set, RecordingUrl still NULL, recent rows).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = N'IX_PlivoCXCallLogs_RecordingBackfill'
      AND  object_id = OBJECT_ID(N'dbo.PlivoCXCallLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PlivoCXCallLogs_RecordingBackfill]
        ON [dbo].[PlivoCXCallLogs] ([CallEndedAt])
        INCLUDE ([PlivoCallUuid], [RecordingUrl]);
END
GO
