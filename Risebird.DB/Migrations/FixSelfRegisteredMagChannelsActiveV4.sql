-- ============================================================================
-- Migration: Fix MAG self-registered interviewers that went ACTIVE in the panel
--            without the manual review / acknowledgement step.
--
-- Bug (found during testing): a prospect who self-registers through the MAG
-- application link (Interviewer/MAGApply -> usp_AddInternalInterviewerToIRFPostPositiveVerdict)
-- was inserted into TransInterviewer with IsActive = 1 / ActivatedAt = GETDATE()
-- for non-locked IRFs. That made them appear ACTIVE in the skill-channel panel
-- immediately, bypassing the policy that channels are added AVAILABLE ONLY and
-- an operator must manually review & activate each one.
-- (Examples reported: NaukriProspectiveInterviewer #13507 krskolluri@gmail.com,
--  #13637 dharma.kaparapu@gmail.com.)
--
-- The stored proc has been corrected to insert IsActive = 0 / ActivatedAt = NULL.
-- This migration repairs the rows that were already created the wrong way.
--
-- Scope (deliberately narrow & safe):
--   * Only channels whose Source = 'Internal MAG Link' (the self-registration path).
--   * Only where the interviewer maps to a Naukri prospect that self-registered.
--   * Only rows currently IsActive = 1.
-- It does NOT touch operator-assigned channels or general direct-IRF applicants.
--
-- Idempotent: re-running affects nothing once the rows are already IsActive = 0.
-- ============================================================================

SET NOCOUNT ON;

-- ── 1. BEFORE snapshot — what we are about to change ────────────────────────
PRINT '--- Affected MAG self-registered channels (BEFORE) ---';
SELECT ti.Id AS TransInterviewerId, ti.UserId, ul.EmailId, ti.IRFId,
       ti.IsAvailable, ti.IsActive, ti.ActivatedAt, ti.[Source],
       np.Id AS NaukriRecordId, np.FunnelStage, np.SelfRegistrationCompleted
FROM TransInterviewer ti
JOIN UserList ul ON ul.id = ti.UserId
JOIN NaukriProspectiveInterviewer np
     ON (np.OnboardedUserId = ti.UserId
         OR LOWER(LTRIM(RTRIM(np.ParsedEmail))) = LOWER(LTRIM(RTRIM(ul.EmailId))))
    AND ISNULL(np.SelfRegistrationCompleted, 0) = 1
WHERE ti.[Source] = 'Internal MAG Link'
  AND ISNULL(ti.IsActive, 0) = 1;

-- ── 2. Repair — demote to AVAILABLE ONLY, pending manual operator activation ─
UPDATE ti
SET ti.IsActive    = 0,
    ti.ActivatedAt = NULL
FROM TransInterviewer ti
JOIN UserList ul ON ul.id = ti.UserId
JOIN NaukriProspectiveInterviewer np
     ON (np.OnboardedUserId = ti.UserId
         OR LOWER(LTRIM(RTRIM(np.ParsedEmail))) = LOWER(LTRIM(RTRIM(ul.EmailId))))
    AND ISNULL(np.SelfRegistrationCompleted, 0) = 1
WHERE ti.[Source] = 'Internal MAG Link'
  AND ISNULL(ti.IsActive, 0) = 1;

PRINT CONCAT('Channels demoted to available-only: ', @@ROWCOUNT);

-- ── 3. AFTER snapshot — verify the repair ───────────────────────────────────
PRINT '--- MAG self-registered channels (AFTER) ---';
SELECT ti.Id AS TransInterviewerId, ti.UserId, ul.EmailId, ti.IRFId,
       ti.IsAvailable, ti.IsActive, ti.ActivatedAt, ti.[Source]
FROM TransInterviewer ti
JOIN UserList ul ON ul.id = ti.UserId
JOIN NaukriProspectiveInterviewer np
     ON (np.OnboardedUserId = ti.UserId
         OR LOWER(LTRIM(RTRIM(np.ParsedEmail))) = LOWER(LTRIM(RTRIM(ul.EmailId))))
    AND ISNULL(np.SelfRegistrationCompleted, 0) = 1
WHERE ti.[Source] = 'Internal MAG Link';
GO
