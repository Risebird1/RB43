/*
    One-off backfill: PlivoCXCallLogs.PlivoCallUuid for calls made BEFORE recording capture was deployed.

    Why this is possible: every Plivo CX hangup event is logged (first 500 chars of the body) in
    PlivoCXWebhookCheckpoints as CheckpointName = 'hangup_body_read'. That body contains both
        "call_uuid":"<uuid>"   and   "conversation_id":"<id>"
    and HangupCallback writes that conversation_id to PlivoCXCallLogs.PlivoCXConversationId,
    so each call log can be matched to its call UUID.

    Safety:
      - Only fills rows where PlivoCallUuid IS NULL (never overwrites).
      - Skips any conversation that maps to more than one UUID (none found at time of writing).
      - Runs in a transaction; the preview SELECT shows what will change before the UPDATE.

    Prerequisite: Risebird.DB/Tables/PlivoCXCallLogs_Add_Recording.sql has been applied.

    After this: recordings for these calls are filled by Backfill_PlivoCX_RecordingUrl.sql (generated
    separately), because PlivoCXRecordingBackfillJob only looks back 24 hours.
*/
SET NOCOUNT ON;

DECLARE @uk VARCHAR(20) = 'call_uuid' + CHAR(34) + ':' + CHAR(34);        -- call_uuid":"
DECLARE @ck VARCHAR(30) = 'conversation_id' + CHAR(34) + ':' + CHAR(34);  -- conversation_id":"

IF OBJECT_ID('tempdb..#HangupUuid') IS NOT NULL DROP TABLE #HangupUuid;

WITH raw AS (
    SELECT AdditionalData AS d,
           CHARINDEX(@uk, AdditionalData) AS up,
           CHARINDEX(@ck, AdditionalData) AS cp
    FROM   dbo.PlivoCXWebhookCheckpoints
    WHERE  CheckpointName = 'hangup_body_read'
), parsed AS (
    SELECT SUBSTRING(d, up + LEN(@uk), 36) AS CallUuid,
           SUBSTRING(d, cp + LEN(@ck), CHARINDEX(CHAR(34), d, cp + LEN(@ck)) - (cp + LEN(@ck))) AS ConvId
    FROM   raw
    WHERE  up > 0 AND cp > 0 AND CHARINDEX(CHAR(34), d, cp + LEN(@ck)) > 0
)
SELECT ConvId, MIN(CallUuid) AS CallUuid
INTO   #HangupUuid
FROM   parsed
WHERE  CallUuid LIKE '[0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f]-%'
GROUP BY ConvId
HAVING COUNT(DISTINCT CallUuid) = 1;          -- unambiguous matches only

-- ── Preview ────────────────────────────────────────────────────────────────
SELECT COUNT(*) AS RowsToUpdate,
       SUM(CASE WHEN pcl.CallStatus = 'completed' THEN 1 ELSE 0 END) AS CompletedCalls,
       MIN(pcl.CreatedAt) AS OldestCall,
       MAX(pcl.CreatedAt) AS NewestCall
FROM   dbo.PlivoCXCallLogs pcl
JOIN   #HangupUuid h ON h.ConvId = pcl.PlivoCXConversationId
WHERE  pcl.PlivoCallUuid IS NULL;

SELECT TOP 10 pcl.Id, pcl.RecipientName, pcl.CallStatus, pcl.CreatedAt, h.CallUuid
FROM   dbo.PlivoCXCallLogs pcl
JOIN   #HangupUuid h ON h.ConvId = pcl.PlivoCXConversationId
WHERE  pcl.PlivoCallUuid IS NULL
ORDER BY pcl.Id DESC;

-- ── Update ─────────────────────────────────────────────────────────────────
BEGIN TRAN;

UPDATE pcl
SET    pcl.PlivoCallUuid = h.CallUuid
FROM   dbo.PlivoCXCallLogs pcl
JOIN   #HangupUuid h ON h.ConvId = pcl.PlivoCXConversationId
WHERE  pcl.PlivoCallUuid IS NULL;

SELECT @@ROWCOUNT AS RowsUpdated;

COMMIT TRAN;

DROP TABLE #HangupUuid;
GO
