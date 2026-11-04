-- One-off backfill (call opt-out / MAG exclusion): interviewers a Risebird user inactivated BEFORE
-- usp_ExcludeInterviewerFromMAG existed are still in their MAGs. This removes them, with a snapshot so
-- usp_ReactivateInterviewer (or usp_RestoreInterviewerToMAG <userId>, 'ManualInactive', ...) can put them back.
--
-- Candidates: UserList.InactivationType = ManualBlock | ManualRemove | ManualFlag, still inactive
-- (IsActive = 0 or IsInterviewerAccess = 0), still holding assessment groups, and no open ManualInactive
-- exclusion. Auto-inactivations (NoShow, Unsubscribe, PendingAcknowledgement, ...) are left alone.
-- Older inactivations with no InactivationType: ops lists their emails in @extraEmails.
--
-- Run after Tables/InterviewerMAGExclusions.sql and usp_ExcludeInterviewerFromMAG.sql.
-- 1. Run the PREVIEW and review the list with ops.  2. Uncomment and run the EXECUTE block.

DECLARE @extraEmails TABLE (EmailId varchar(200) PRIMARY KEY)
-- INSERT INTO @extraEmails (EmailId) VALUES ('someone@example.com'), ('other@example.com')

;WITH Targets AS (
    SELECT ul.id AS UserId, ul.EmailId, ul.IsActive, ul.IsInterviewerAccess,
           ul.InactivationType, ul.InactivatedBy, ul.InactivatedAt, ip.AssessmentGroupId
    FROM UserList ul
    JOIN InterviewerProfile ip ON ip.UserId = ul.id
    WHERE NULLIF(LTRIM(RTRIM(ip.AssessmentGroupId)), '') IS NOT NULL
      AND (ISNULL(ul.IsActive, 1) = 0 OR ISNULL(ul.IsInterviewerAccess, 1) = 0)
      AND (ul.InactivationType IN ('ManualBlock', 'ManualRemove', 'ManualFlag')
           OR ul.EmailId IN (SELECT EmailId FROM @extraEmails))
      AND NOT EXISTS (SELECT 1 FROM InterviewerMAGExclusions e
                      WHERE e.UserId = ul.id AND e.Reason = 'ManualInactive' AND e.RestoredOn IS NULL)
)
-- PREVIEW
SELECT * FROM Targets ORDER BY InactivatedAt DESC

/* EXECUTE — uncomment after the preview has been reviewed (re-declare @extraEmails above if run separately)

DECLARE @Now datetime = DATEADD(MINUTE, 330, GETUTCDATE())
DECLARE @By varchar(200) = 'Backfill_ExcludeManuallyInactiveInterviewersFromMAG'
DECLARE @Uid int

DECLARE bf_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT ul.id
    FROM UserList ul
    JOIN InterviewerProfile ip ON ip.UserId = ul.id
    WHERE NULLIF(LTRIM(RTRIM(ip.AssessmentGroupId)), '') IS NOT NULL
      AND (ISNULL(ul.IsActive, 1) = 0 OR ISNULL(ul.IsInterviewerAccess, 1) = 0)
      AND (ul.InactivationType IN ('ManualBlock', 'ManualRemove', 'ManualFlag')
           OR ul.EmailId IN (SELECT EmailId FROM @extraEmails))
      AND NOT EXISTS (SELECT 1 FROM InterviewerMAGExclusions e
                      WHERE e.UserId = ul.id AND e.Reason = 'ManualInactive' AND e.RestoredOn IS NULL)

OPEN bf_cur
FETCH NEXT FROM bf_cur INTO @Uid
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC usp_ExcludeInterviewerFromMAG @Uid, 'ManualInactive', @By, @Now
    FETCH NEXT FROM bf_cur INTO @Uid
END
CLOSE bf_cur
DEALLOCATE bf_cur

SELECT COUNT(*) AS Excluded FROM InterviewerMAGExclusions WHERE PerformedBy = @By
*/
