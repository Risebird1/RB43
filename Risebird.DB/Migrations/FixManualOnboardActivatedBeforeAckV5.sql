-- ============================================================================
-- Migration: Deactivate interviewer accounts that were made ACTIVE during manual
--            NaukriOnboarding BEFORE the prospect accepted the acknowledgement email.
--
-- Bug (found during testing): the manual onboard action (NaukriOnboarding ->
-- OnboardInterviewer) created the InterviewerProfile / UserList with IsActive = 1.
-- That made the prospect a live, active interviewer the moment the profile was
-- created and the acknowledgement email was sent — i.e. "onboarded without
-- acknowledging". Onboarding is supposed to complete ONLY when the prospect clicks
-- the accept button in that email (InterviewerAcknowledgementResponse status=1),
-- which advances the funnel to 'Onboarded' and activates the account.
-- (Examples reported: #13520 tathagathashashank@gmail.com, #13516
--  shreya.paul93@gmail.com, #13594 sameertiwari1474@gmail.com.)
--
-- The controller now creates the account with IsActive = 0. This migration repairs
-- accounts already activated the wrong way.
--
-- Scope (deliberately narrow & safe): only UserList accounts that
--   * are linked to a Naukri prospect (OnboardedUserId), AND
--   * had a profile created (OnboardingCompleted = 1), AND
--   * have NOT reached the 'Onboarded' funnel stage (i.e. not yet accepted), AND
--   * are NOT self-registered (SelfRegistration is a genuine accept), AND
--   * are currently IsActive = 1.
-- It never touches genuinely-onboarded interviewers (FunnelStage = 'Onboarded').
--
-- Idempotent: re-running affects nothing once the rows are already IsActive = 0.
-- ============================================================================

SET NOCOUNT ON;

-- ── 1. BEFORE snapshot — accounts we are about to deactivate ─────────────────
PRINT '--- Accounts active before acknowledgement (BEFORE) ---';
SELECT np.Id AS NaukriRecordId, np.CandidateName, ul.id AS UserId, ul.EmailId,
       ul.IsActive, np.FunnelStage, dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) AS CanonStage,
       np.OnboardingCompleted, np.SelfRegistrationCompleted
FROM UserList ul
JOIN NaukriProspectiveInterviewer np ON np.OnboardedUserId = ul.id
WHERE ISNULL(np.OnboardingCompleted, 0) = 1
  AND ISNULL(np.SelfRegistrationCompleted, 0) = 0
  AND dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) <> 'Onboarded'
  AND ISNULL(ul.IsActive, 0) = 1;

-- ── 2. Repair — deactivate, pending the prospect's acknowledgement acceptance ─
UPDATE ul
SET ul.IsActive        = 0,
    ul.InactivationType = 'PendingAcknowledgement',
    ul.InactivatedBy    = 'System (NaukriOnboarding acknowledgement gate)',
    ul.InactivatedAt    = GETDATE()
FROM UserList ul
JOIN NaukriProspectiveInterviewer np ON np.OnboardedUserId = ul.id
WHERE ISNULL(np.OnboardingCompleted, 0) = 1
  AND ISNULL(np.SelfRegistrationCompleted, 0) = 0
  AND dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) <> 'Onboarded'
  AND ISNULL(ul.IsActive, 0) = 1;

PRINT CONCAT('Accounts deactivated (pending acknowledgement): ', @@ROWCOUNT);

-- ── 3. AFTER snapshot — verify the repair ───────────────────────────────────
PRINT '--- Linked accounts (AFTER) ---';
SELECT np.Id AS NaukriRecordId, ul.id AS UserId, ul.EmailId, ul.IsActive,
       dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) AS CanonStage
FROM UserList ul
JOIN NaukriProspectiveInterviewer np ON np.OnboardedUserId = ul.id
WHERE ISNULL(np.OnboardingCompleted, 0) = 1
  AND ISNULL(np.SelfRegistrationCompleted, 0) = 0;
GO
