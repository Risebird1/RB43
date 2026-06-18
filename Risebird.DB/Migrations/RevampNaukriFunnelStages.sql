-- ============================================================================
-- Migration: NaukriOnboarding funnel-stage revamp
--
-- Revised funnel model (FunnelStage canonical values):
--   Happy path (monotonic): Parsed -> MAGMapped -> InvitationSent -> AwaitingResponse
--                            -> IntentConfirmed -> EmploymentVerified -> ResumeFitment
--                            -> AcknowledgementSent -> Onboarded
--   Terminal exits (off the happy path, rank 0):
--       Duplicate       — email already exists in the system (any profile) — first-step screening
--       NotInterested   — prospect explicitly declined
--       Unsubscribe     — prospect asked to drop their profile / unsubscribe
--       Cold            — no response after the outreach budget (1 email + up to 5 calls) is spent
--
-- Changes vs. the previous model:
--   • NEW first stage 'Duplicate' (was surfaced only as AutomationStatus='ManualRequired').
--   • 'IntentCollection' (the waiting phase) is folded into the new 'AwaitingResponse'.
--   • 'IntentConfirmed' now means a POSITIVE confirmation only; negative confirmations move
--     to the dedicated terminal stages 'NotInterested' / 'Unsubscribe'.
--   • NEW terminal 'Cold' funnel stage (previously only an AutomationStatus).
--
-- Idempotent: safe to re-run. Onboarded / self-registered rows are never reclassified.
-- ============================================================================

SET NOCOUNT ON;

-- A) Duplicates exit at the first-step screening stage 'Duplicate'.
--    (An email can exist only once in UserList — as ANY profile.)
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'Duplicate',
    UpdatedAt   = GETUTCDATE()
WHERE ISNULL(IsExistingInterviewer, 0) = 1
  AND ISNULL(OnboardingCompleted, 0) = 0
  AND ISNULL(SelfRegistrationCompleted, 0) = 0
  AND ISNULL(FunnelStage, '') NOT IN ('Onboarded', 'Duplicate');
GO

-- B) Fold the legacy 'IntentCollection' waiting phase into 'AwaitingResponse'.
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'AwaitingResponse',
    UpdatedAt   = GETUTCDATE()
WHERE FunnelStage = 'IntentCollection';
GO

-- C) Split negative confirmations out of 'IntentConfirmed' (now positive-only) and
--    out of 'AwaitingResponse', into their dedicated terminal stages.
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = CASE LOWER(LTRIM(RTRIM(ISNULL(Intent, ''))))
                      WHEN 'notinterested' THEN 'NotInterested'
                      WHEN 'unsubscribe'   THEN 'Unsubscribe'
                  END,
    TicketStatus = 'Closed',
    -- Also close the legacy operational Status so the automation eligibility query
    -- (which still gates on Status IN ('Open','InProgress')) can never re-pick a resolved exit.
    Status       = 'Closed',
    UpdatedAt    = GETUTCDATE()
WHERE FunnelStage IN ('IntentConfirmed', 'AwaitingResponse')
  AND LOWER(LTRIM(RTRIM(ISNULL(Intent, '')))) IN ('notinterested', 'unsubscribe')
  AND ISNULL(OnboardingCompleted, 0) = 0;
GO

-- D) No-response prospects whose outreach budget was exhausted (AutomationStatus='Cold')
--    move to the terminal 'Cold' funnel stage — unless they already have a more specific
--    outcome or have progressed further down the happy path.
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'Cold',
    TicketStatus = 'Closed',
    Status       = 'Closed',
    UpdatedAt    = GETUTCDATE()
WHERE LOWER(LTRIM(RTRIM(ISNULL(AutomationStatus, '')))) = 'cold'
  AND ISNULL(OnboardingCompleted, 0) = 0
  AND ISNULL(SelfRegistrationCompleted, 0) = 0
  AND ISNULL(FunnelStage, '') NOT IN (
        'Onboarded', 'Duplicate', 'NotInterested', 'Unsubscribe',
        'EmploymentVerified', 'ResumeFitment', 'AcknowledgementSent');
GO
