-- UNDO for ActivateNonLockedFunnelChannelsV8.sql
--
-- Only needed if you ran ActivateNonLockedFunnelChannelsV8.sql with @CommitChanges = 1 (committed).
-- If you ran it with the default @CommitChanges = 0 (dry run) it was rolled back and there is
-- NOTHING to undo.
--
-- HOW THIS TARGETS THE RIGHT ROWS
-- The forward migration set, for each affected row:
--     IsActive    = 1
--     ActivatedAt = (its run time, in IST)   -- because those rows had ActivatedAt = NULL before
-- So we revert in-scope rows whose ActivatedAt falls inside the window when you ran the migration.
-- Keep the window TIGHT (bracketing your actual run) so this never deactivates a channel that was
-- legitimately activated by normal onboarding around the same time.
--
-- Defaults to a DRY RUN: review "RowsThatWillRevert", then set @CommitChanges = 1 to apply.

SET NOCOUNT ON;

DECLARE @CommitChanges BIT = 0;   -- DRY RUN by default; set to 1 to actually revert

-- >>> ADJUST THESE to bracket WHEN you ran ActivateNonLockedFunnelChannelsV8.sql (server/IST time). <<<
DECLARE @RunFrom DATETIME = '2026-06-29 20:00:00';
DECLARE @RunTo   DATETIME = '2026-06-29 21:30:00';

DECLARE @FunnelSources TABLE (Src VARCHAR(50) PRIMARY KEY);
INSERT INTO @FunnelSources (Src) VALUES
    ('MAG-Direct'), ('Naukri-Adopt'), ('EmailAvailability'),
    ('Internal IRF Link'), ('Internal MAG Link');

BEGIN TRAN;

    -- Diagnostic: what will be reverted, by source.
    SELECT ti.[Source], COUNT(*) AS RowsThatWillRevert
    FROM TransInterviewer ti
    INNER JOIN IRF irf ON irf.id = ti.IRFId
    LEFT JOIN UserList ul ON ul.id = ti.UserId
    WHERE ti.[Source] IN (SELECT Src FROM @FunnelSources)
      AND ISNULL(irf.DmIvLockNew, 0) = 0
      AND ti.IsActive = 1
      AND ti.ActivatedAt >= @RunFrom AND ti.ActivatedAt <= @RunTo
      AND ul.InactivationType IS NULL
    GROUP BY ti.[Source];

    UPDATE ti
    SET ti.IsActive    = 0,
        ti.ActivatedAt = NULL
    FROM TransInterviewer ti
    INNER JOIN IRF irf ON irf.id = ti.IRFId
    LEFT JOIN UserList ul ON ul.id = ti.UserId
    WHERE ti.[Source] IN (SELECT Src FROM @FunnelSources)
      AND ISNULL(irf.DmIvLockNew, 0) = 0
      AND ti.IsActive = 1
      AND ti.ActivatedAt >= @RunFrom AND ti.ActivatedAt <= @RunTo
      AND ul.InactivationType IS NULL;

    DECLARE @Reverted INT = @@ROWCOUNT;
    SELECT @Reverted AS RowsReverted;

IF @CommitChanges = 1
BEGIN
    COMMIT;
    PRINT 'UndoActivateNonLockedFunnelChannelsV8: committed (reverted).';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'UndoActivateNonLockedFunnelChannelsV8: DRY RUN — rolled back (set @CommitChanges = 1 to apply).';
END
GO
