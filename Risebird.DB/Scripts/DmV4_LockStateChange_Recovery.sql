/*
================================================================================
DM V4 — One-time recovery after lock-state-change multi-constraint violation
================================================================================
Run ONCE in production AFTER deploying the code fix (dm-v4-layout.js + helpers).

INVESTIGATION SUMMARY (Step 1 — lock handler)
---------------------------------------------
Backend lock toggle: POST /IRF/SaveDmIvPolicy → usp_UpdateDmIvPolicy
  WRITES ONLY: IRF.DmIvLockNew, IRF.DmAssignmentPolicy, IRF.DmDefaultInterviewCap, IRF.ModifiedOn
  DOES NOT touch TransInterviewer.

Root cause of QA symptoms: frontend dm-v4-layout.js
  - updateIvRowStatus() overwrote data-is-active from derived vet state on lock
  - normalizeVetRowAttrs() rewrote data-rb-vet / data-client-vet on page init
  - shouldShowUnlimitedCap() showed "Unlimited" when explicit cap = 0

AUDIT TRAIL
-----------
No TransInterviewer row-level audit/history table exists in this codebase.
Restoration uses safe defaults per spec when prior values are unavailable:
  IsActive = 1, IsAvailable = 1, InterviewCapacity = NULL (unlimited)

USAGE
-----
1. Set @LockFlipSince to the earliest date of suspected lock flips (or NULL for all).
2. Run STEP A (investigation) — review counts before mutating.
3. Run STEP B (restore) inside an explicit transaction; COMMIT only after review.
================================================================================
*/

SET NOCOUNT ON;

DECLARE @LockFlipSince datetime = '2025-01-01';  -- adjust per environment
DECLARE @ModifiedWindowMinutes int = 15;

IF OBJECT_ID('tempdb..#DmV4LockRecoveryLog') IS NOT NULL DROP TABLE #DmV4LockRecoveryLog;
CREATE TABLE #DmV4LockRecoveryLog (
    TransInterviewerId int NOT NULL,
    IRFId int NOT NULL,
    FieldName varchar(32) NOT NULL,
    OldValue sql_variant NULL,
    NewValue sql_variant NULL,
    RestoreSource varchar(32) NOT NULL
);

/* STEP A — Investigation: channels locked recently + suspect row changes */
;WITH LockFlips AS (
    SELECT
        irf.id AS IRFId,
        irf.ModifiedOn AS LockModifiedOn
    FROM IRF irf
    WHERE ISNULL(irf.DmIvLockNew, 0) = 1
      AND (@LockFlipSince IS NULL OR irf.ModifiedOn >= @LockFlipSince)
),
SuspectRows AS (
    SELECT
        ti.id AS TransInterviewerId,
        ti.IRFId,
        ti.IsActive,
        ti.IsAvailable,
        ti.InterviewCapacity,
        ti.IsSMEVerified,
        ti.ClientAction,
        ti.AddedAt,
        ti.ModifiedAt,
        lf.LockModifiedOn
    FROM TransInterviewer ti
    INNER JOIN LockFlips lf ON lf.IRFId = ti.IRFId
    WHERE ti.AddedAt < lf.LockModifiedOn
      AND ti.ModifiedAt BETWEEN DATEADD(MINUTE, -@ModifiedWindowMinutes, lf.LockModifiedOn)
                            AND DATEADD(MINUTE, @ModifiedWindowMinutes, lf.LockModifiedOn)
      AND (
            ti.IsActive = 0
            OR ti.IsAvailable = 0
            OR ti.InterviewCapacity = 0
          )
      AND (
            ISNULL(ti.IsSMEVerified, 0) = 1
            OR ti.ClientAction = 'Shortlisted'
            OR ti.ActivatedAt IS NOT NULL
          )
)
SELECT
    COUNT(*) AS SuspectRowCount,
    COUNT(DISTINCT IRFId) AS AffectedChannelCount
FROM SuspectRows;

SELECT TOP 500 *
FROM SuspectRows
ORDER BY LockModifiedOn DESC, TransInterviewerId;

/*
STEP B — Restore (uncomment and run in a transaction after reviewing STEP A)

BEGIN TRAN;

DECLARE @RestoreSource varchar(32) = 'safe-default';

;WITH LockFlips AS (
    SELECT irf.id AS IRFId, irf.ModifiedOn AS LockModifiedOn
    FROM IRF irf
    WHERE ISNULL(irf.DmIvLockNew, 0) = 1
      AND (@LockFlipSince IS NULL OR irf.ModifiedOn >= @LockFlipSince)
),
ToRestore AS (
    SELECT
        ti.id AS TransInterviewerId,
        ti.IRFId,
        ti.IsActive AS OldIsActive,
        ti.IsAvailable AS OldIsAvailable,
        ti.InterviewCapacity AS OldInterviewCapacity
    FROM TransInterviewer ti
    INNER JOIN LockFlips lf ON lf.IRFId = ti.IRFId
    WHERE ti.AddedAt < lf.LockModifiedOn
      AND ti.ModifiedAt BETWEEN DATEADD(MINUTE, -@ModifiedWindowMinutes, lf.LockModifiedOn)
                            AND DATEADD(MINUTE, @ModifiedWindowMinutes, lf.LockModifiedOn)
      AND (
            ti.IsActive = 0 OR ti.IsAvailable = 0 OR ti.InterviewCapacity = 0
          )
      AND (
            ISNULL(ti.IsSMEVerified, 0) = 1
            OR ti.ClientAction = 'Shortlisted'
            OR ti.ActivatedAt IS NOT NULL
          )
)
INSERT INTO #DmV4LockRecoveryLog (TransInterviewerId, IRFId, FieldName, OldValue, NewValue, RestoreSource)
SELECT TransInterviewerId, IRFId, 'IsActive', OldIsActive, CAST(1 AS bit), @RestoreSource
FROM ToRestore WHERE OldIsActive = 0
UNION ALL
SELECT TransInterviewerId, IRFId, 'IsAvailable', OldIsAvailable, CAST(1 AS bit), @RestoreSource
FROM ToRestore WHERE OldIsAvailable = 0
UNION ALL
SELECT TransInterviewerId, IRFId, 'InterviewCapacity', OldInterviewCapacity, NULL, @RestoreSource
FROM ToRestore WHERE OldInterviewCapacity = 0;

UPDATE ti
SET
    IsActive = CASE WHEN ti.IsActive = 0 THEN 1 ELSE ti.IsActive END,
    IsAvailable = CASE WHEN ti.IsAvailable = 0 THEN 1 ELSE ti.IsAvailable END,
    InterviewCapacity = CASE WHEN ti.InterviewCapacity = 0 THEN NULL ELSE ti.InterviewCapacity END,
    ModifiedAt = GETDATE(),
    ModifiedBy = 'DmV4_LockRecovery'
FROM TransInterviewer ti
INNER JOIN (
    SELECT DISTINCT TransInterviewerId FROM #DmV4LockRecoveryLog
) r ON r.TransInterviewerId = ti.id;

SELECT * FROM #DmV4LockRecoveryLog ORDER BY TransInterviewerId, FieldName;

-- ROLLBACK TRAN;  -- use during dry-run
-- COMMIT TRAN;
*/
