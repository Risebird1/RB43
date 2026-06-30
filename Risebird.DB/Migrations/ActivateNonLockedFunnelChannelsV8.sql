-- Corrective migration: activate NON-LOCKED skill channels that the old funnel policy forced inactive.
--
-- Background: the NaukriOnboarding and InterviewerAvailability flows previously inserted every
-- TransInterviewer row as Available-only (IsActive = 0), regardless of the channel's Interviewer
-- Lock. The correct DM-V4 policy (IRF.DmIvLockNew) is:
--     Locked  (DmIvLockNew = 1) → IsActive = 0  (pending manual RB vetting)
--     Unlocked(DmIvLockNew = 0) → IsActive = 1  (auto-activated)
-- The stored procedures have been fixed going forward; this script realigns rows already created
-- by those flows.
--
-- SAFETY SCOPING — only rows that ALL of the following are true are touched:
--   • Source is one of the funnel mapping sources (so we never touch bulk-add / other paths).
--   • The channel's IRF is currently NON-LOCKED (DmIvLockNew = 0 / NULL).
--   • The channel is currently inactive (IsActive = 0) and Available (IsAvailable = 1).
--   • The linked UserList account was NOT deliberately inactivated (InactivationType IS NULL).
--     This avoids re-activating channels for interviewers who were intentionally dropped/disabled.
--   • The channel was CREATED ON/AFTER @OnlyChannelsAddedAfter (REQUIRED — see below).
--
-- *** WHY @OnlyChannelsAddedAfter IS MANDATORY ***
-- The funnel sources (esp. 'Internal MAG Link' / 'Internal IRF Link') hold YEARS of historical
-- self-registration rows that are inactive for perfectly legitimate reasons (never vetted, never
-- acknowledged, abandoned applications, etc.). Without a date cutoff this script would mass-activate
-- that entire backlog (tens of thousands of rows) — which is NOT what the SP fix intended.
-- Our blanket-IsActive=0 bug only affected channels created AFTER those SPs were deployed, so set
-- @OnlyChannelsAddedAfter to that deployment date/time. The script refuses to run without it.
--
-- Review the "RowsThatWillChange" diagnostic before committing. Set @CommitChanges = 1 to apply.

SET NOCOUNT ON;

DECLARE @CommitChanges BIT = 0;                 -- DRY RUN by default: review diagnostics, then set to 1
DECLARE @OnlyChannelsAddedAfter DATETIME = NULL; -- REQUIRED: set to when the blanket-IsActive=0 SPs were deployed (IST)

IF @OnlyChannelsAddedAfter IS NULL
BEGIN
    PRINT 'ABORTED: set @OnlyChannelsAddedAfter to the deployment date of the blanket-IsActive=0 SPs first.';
    PRINT 'Leaving it NULL would mass-activate the entire historical self-registration backlog.';
    RETURN;
END

DECLARE @FunnelSources TABLE (Src VARCHAR(50) PRIMARY KEY);
INSERT INTO @FunnelSources (Src) VALUES
    ('MAG-Direct'), ('Naukri-Adopt'), ('EmailAvailability'),
    ('Internal IRF Link'), ('Internal MAG Link');

BEGIN TRAN;

    -- Diagnostic: how many rows match, broken down by source.
    SELECT ti.[Source], COUNT(*) AS RowsThatWillChange
    FROM TransInterviewer ti
    INNER JOIN IRF irf ON irf.id = ti.IRFId
    LEFT JOIN UserList ul ON ul.id = ti.UserId
    WHERE ti.[Source] IN (SELECT Src FROM @FunnelSources)
      AND ISNULL(irf.DmIvLockNew, 0) = 0
      AND ISNULL(ti.IsActive, 0) = 0
      AND ISNULL(ti.IsAvailable, 0) = 1
      AND ul.InactivationType IS NULL
      AND ti.AddedAt >= @OnlyChannelsAddedAfter
    GROUP BY ti.[Source];

    UPDATE ti
    SET ti.IsActive    = 1,
        ti.ActivatedAt = ISNULL(ti.ActivatedAt, DATEADD(MINUTE, 330, GETDATE()))
    FROM TransInterviewer ti
    INNER JOIN IRF irf ON irf.id = ti.IRFId
    LEFT JOIN UserList ul ON ul.id = ti.UserId
    WHERE ti.[Source] IN (SELECT Src FROM @FunnelSources)
      AND ISNULL(irf.DmIvLockNew, 0) = 0
      AND ISNULL(ti.IsActive, 0) = 0
      AND ISNULL(ti.IsAvailable, 0) = 1
      AND ul.InactivationType IS NULL
      AND ti.AddedAt >= @OnlyChannelsAddedAfter;

    DECLARE @Updated INT = @@ROWCOUNT;
    SELECT @Updated AS RowsActivated;

IF @CommitChanges = 1
BEGIN
    COMMIT;
    PRINT 'ActivateNonLockedFunnelChannelsV8: committed.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'ActivateNonLockedFunnelChannelsV8: DRY RUN — rolled back (set @CommitChanges = 1 to apply).';
END
GO
