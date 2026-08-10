-- =============================================================================
-- usp_PurgeOldData
--
-- Generic daily purge job. Deletes rows older than the configured retention
-- window from every high-churn table across the platform.
-- Called once daily by the PurgeOldDataJob Azure Timer Function.
--
-- Parameters
--   @VapiPlivoRetentionDays  INT = 30   — Vapi / Plivo call + debug + checkpoint tables
--   @OpenAIRetentionDays     INT = 60   — OpenAI API log tables
--
-- Returns a single-row result set with the deleted-row count per table so
-- the caller can log a full summary without a round-trip per table.
--
-- ── Vapi / Plivo tables (30-day default) ─────────────────────────────────────
--   VapiWebhookDebugLogs      (RequestTimestamp)
--   VapiWebhookCheckpoints    (Timestamp)
--   PlivoWebhookDebugLogs     (RequestTimestamp)
--   PlivoCXWebhookDebugLogs   (CreatedAt)
--   PlivoCXWebhookCheckpoints (Timestamp)
--   PlivoWhatsAppDebugLogs    (CreatedAt)
--   PlivoWhatsAppCheckpoints  (Timestamp)
--   VapiCallLogs              (CreatedAt)
--   PlivoCallLogs             (CreatedAt)
--   PlivoCXCallLogs           (CreatedAt)
--   PlivoWhatsAppLogs         (CreatedAt)
--
-- ── OpenAI / Azure cost tables (60-day default) ───────────────────────────────
--   OpenAIApiLogs             (LogDate)
-- =============================================================================

-- Drop the old procedure name if it still exists from a previous deployment
IF OBJECT_ID(N'[dbo].[usp_DeleteOldVapiPlivoData]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_DeleteOldVapiPlivoData];
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_PurgeOldData]
    @VapiPlivoRetentionDays INT = 30,
    @OpenAIRetentionDays    INT = 60
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @VapiPlivoCutoff DATETIME  = DATEADD(DAY, -@VapiPlivoRetentionDays, GETUTCDATE()),
        @OpenAICutoff    DATETIME2 = DATEADD(DAY, -@OpenAIRetentionDays,    GETUTCDATE());

    -- ── Row-count accumulators ────────────────────────────────────────────────
    DECLARE
        -- Vapi / Plivo
        @VapiDebugRows         INT = 0,
        @VapiCheckpointRows    INT = 0,
        @PlivoDebugRows        INT = 0,
        @PlivoCxDebugRows      INT = 0,
        @PlivoCxCheckpointRows INT = 0,
        @WaDebugRows           INT = 0,
        @WaCheckpointRows      INT = 0,
        @VapiCallLogRows       INT = 0,
        @PlivoCallLogRows      INT = 0,
        @PlivoCxCallLogRows    INT = 0,
        @WaLogRows             INT = 0,
        -- OpenAI / Azure cost
        @OpenAIApiLogRows      INT = 0;

    -- ── Vapi / Plivo: debug + checkpoint tables (highest churn) ──────────────

    DELETE FROM [dbo].[VapiWebhookDebugLogs]
    WHERE [RequestTimestamp] < @VapiPlivoCutoff;
    SET @VapiDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[VapiWebhookCheckpoints]
    WHERE [Timestamp] < @VapiPlivoCutoff;
    SET @VapiCheckpointRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWebhookDebugLogs]
    WHERE [RequestTimestamp] < @VapiPlivoCutoff;
    SET @PlivoDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoCXWebhookDebugLogs]
    WHERE [CreatedAt] < @VapiPlivoCutoff;
    SET @PlivoCxDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoCXWebhookCheckpoints]
    WHERE [Timestamp] < @VapiPlivoCutoff;
    SET @PlivoCxCheckpointRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWhatsAppDebugLogs]
    WHERE [CreatedAt] < @VapiPlivoCutoff;
    SET @WaDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWhatsAppCheckpoints]
    WHERE [Timestamp] < @VapiPlivoCutoff;
    SET @WaCheckpointRows = @@ROWCOUNT;

    -- ── Vapi / Plivo: core call / message log tables ──────────────────────────

    DELETE FROM [dbo].[VapiCallLogs]
    WHERE [CreatedAt] < @VapiPlivoCutoff;
    SET @VapiCallLogRows = @@ROWCOUNT;

    --DELETE FROM [dbo].[PlivoCallLogs]
    --WHERE [CreatedAt] < @VapiPlivoCutoff;
    --SET @PlivoCallLogRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoCXCallLogs]
    WHERE [CreatedAt] < @VapiPlivoCutoff;
    SET @PlivoCxCallLogRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWhatsAppLogs]
    WHERE [CreatedAt] < @VapiPlivoCutoff;
    SET @WaLogRows = @@ROWCOUNT;

    -- ── OpenAI / Azure cost tables ────────────────────────────────────────────

    DELETE FROM [dbo].[OpenAIApiLogs]
    WHERE [LogDate] < @OpenAICutoff;
    SET @OpenAIApiLogRows = @@ROWCOUNT;

    -- Mail ---
    delete from [dbo].[SentMailDetails]
    WHERE CreatedAt < @OpenAICutoff;

    delete from [dbo].[SendgridEventData]
    WHERE CreatedAt < @OpenAICutoff;

    -- ── Return summary row ────────────────────────────────────────────────────
    SELECT
        @VapiPlivoCutoff       AS VapiPlivoCutoff,
        @OpenAICutoff          AS OpenAICutoff,
        -- Vapi / Plivo
        @VapiDebugRows         AS VapiWebhookDebugLogs,
        @VapiCheckpointRows    AS VapiWebhookCheckpoints,
        @PlivoDebugRows        AS PlivoWebhookDebugLogs,
        @PlivoCxDebugRows      AS PlivoCXWebhookDebugLogs,
        @PlivoCxCheckpointRows AS PlivoCXWebhookCheckpoints,
        @WaDebugRows           AS PlivoWhatsAppDebugLogs,
        @WaCheckpointRows      AS PlivoWhatsAppCheckpoints,
        @VapiCallLogRows       AS VapiCallLogs,
        @PlivoCallLogRows      AS PlivoCallLogs,
        @PlivoCxCallLogRows    AS PlivoCXCallLogs,
        @WaLogRows             AS PlivoWhatsAppLogs,
        -- OpenAI / Azure cost
        @OpenAIApiLogRows      AS OpenAIApiLogs;
END
GO
