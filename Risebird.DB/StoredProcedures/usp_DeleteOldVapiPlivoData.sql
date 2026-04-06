-- =============================================================================
-- usp_DeleteOldVapiPlivoData
--
-- Removes Vapi and Plivo rows older than @RetentionDays days (default 30).
-- Called once daily by the DeleteOldVapiPlivoData Azure Timer Function.
--
-- Returns a single-row result set with the deleted-row count per table so
-- the caller can log a full summary without a round-trip per table.
--
-- Tables cleaned (debug/checkpoint first — highest churn):
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
-- =============================================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_DeleteOldVapiPlivoData]
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff DATETIME = DATEADD(DAY, -@RetentionDays, GETUTCDATE());

    -- ── Row-count accumulators ────────────────────────────────────────────────
    DECLARE
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
        @WaLogRows             INT = 0;

    -- ── Debug / checkpoint tables (highest churn — delete first) ─────────────

    DELETE FROM [dbo].[VapiWebhookDebugLogs]
    WHERE [RequestTimestamp] < @Cutoff;
    SET @VapiDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[VapiWebhookCheckpoints]
    WHERE [Timestamp] < @Cutoff;
    SET @VapiCheckpointRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWebhookDebugLogs]
    WHERE [RequestTimestamp] < @Cutoff;
    SET @PlivoDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoCXWebhookDebugLogs]
    WHERE [CreatedAt] < @Cutoff;
    SET @PlivoCxDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoCXWebhookCheckpoints]
    WHERE [Timestamp] < @Cutoff;
    SET @PlivoCxCheckpointRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWhatsAppDebugLogs]
    WHERE [CreatedAt] < @Cutoff;
    SET @WaDebugRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWhatsAppCheckpoints]
    WHERE [Timestamp] < @Cutoff;
    SET @WaCheckpointRows = @@ROWCOUNT;

    -- ── Core call / message log tables ───────────────────────────────────────

    DELETE FROM [dbo].[VapiCallLogs]
    WHERE [CreatedAt] < @Cutoff;
    SET @VapiCallLogRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoCallLogs]
    WHERE [CreatedAt] < @Cutoff;
    SET @PlivoCallLogRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoCXCallLogs]
    WHERE [CreatedAt] < @Cutoff;
    SET @PlivoCxCallLogRows = @@ROWCOUNT;

    DELETE FROM [dbo].[PlivoWhatsAppLogs]
    WHERE [CreatedAt] < @Cutoff;
    SET @WaLogRows = @@ROWCOUNT;

    -- ── Return summary row ────────────────────────────────────────────────────
    SELECT
        @Cutoff                AS Cutoff,
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
        @WaLogRows             AS PlivoWhatsAppLogs;
END
GO
