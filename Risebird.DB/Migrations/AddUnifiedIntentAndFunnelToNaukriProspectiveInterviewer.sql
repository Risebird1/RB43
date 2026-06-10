-- ============================================================================
-- Migration: Prospect Onboarding redesign — Phase 1 (data model)
--
-- Adds the columns required to:
--   1. Hold a single UNIFIED INTENT populated by both email replies and AI Plivo
--      calls (replacing the two separate ProspectResponse / PlivoProspectDisposition
--      tracks at the display level).
--   2. SEPARATE the operational ticket status (Open / InProgress / Closed) from the
--      onboarding FUNNEL STAGE (Parsed -> MAGMapped -> ... -> Onboarded).
--   3. Track the ONBOARDING TYPE for reporting (Self / Intent+Auto / Manual).
--
-- Canonical value sets (enforced in application logic, not as DB constraints):
--   Intent          : Interested | NotInterested | Unsubscribe | NoResponse
--   IntentSource     : Email | Call
--   TicketStatus     : Open | InProgress | Closed
--   FunnelStage      : Parsed | MAGMapped | InvitationSent | IntentCollection
--                      | IntentConfirmed | EmploymentVerified | ResumeFitment
--                      | AcknowledgementSent | Onboarded
--   OnboardingType   : SelfOnboarded | IntentAutoProcessed | ManuallyOnboarded
-- ============================================================================

-- ── Unified Intent ──────────────────────────────────────────────────────────
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Intent' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [Intent] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IntentSource' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [IntentSource] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IntentConfirmedAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [IntentConfirmedAt] DATETIME NULL
END
GO

-- ── Ticket status (operational) vs Funnel stage (pipeline position) ──────────
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TicketStatus' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [TicketStatus] VARCHAR(50) NULL DEFAULT 'Open'
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'FunnelStage' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [FunnelStage] VARCHAR(50) NULL
END
GO

-- ── Onboarding type (reporting) ──────────────────────────────────────────────
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OnboardingType' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [OnboardingType] VARCHAR(50) NULL
END
GO

-- ============================================================================
-- BACKFILL existing rows so the new dimensions are sensible from day one.
-- ============================================================================

-- 1) Unified Intent — prefer the call disposition, fall back to the email reply.
UPDATE NaukriProspectiveInterviewer
SET Intent =
        CASE
            WHEN LOWER(ISNULL(PlivoProspectDisposition, '')) = 'interested'      THEN 'Interested'
            WHEN LOWER(ISNULL(PlivoProspectDisposition, '')) = 'not_interested'  THEN 'NotInterested'
            WHEN LOWER(ISNULL(PlivoProspectDisposition, '')) = 'unsubscribe'     THEN 'Unsubscribe'
            WHEN LOWER(ISNULL(ProspectResponse, '')) IN ('interested', 'yes', 'accept', 'accepted') THEN 'Interested'
            WHEN LOWER(ISNULL(ProspectResponse, '')) IN ('not_interested', 'no', 'decline', 'declined') THEN 'NotInterested'
            WHEN LOWER(ISNULL(ProspectResponse, '')) = 'unsubscribe'             THEN 'Unsubscribe'
            ELSE NULL
        END,
    IntentSource =
        CASE
            WHEN LOWER(ISNULL(PlivoProspectDisposition, '')) IN ('interested', 'not_interested', 'unsubscribe') THEN 'Call'
            WHEN ISNULL(ProspectResponse, '') <> '' THEN 'Email'
            ELSE NULL
        END,
    IntentConfirmedAt =
        CASE
            WHEN LOWER(ISNULL(PlivoProspectDisposition, '')) IN ('interested', 'not_interested', 'unsubscribe') THEN PlivoCallAt
            WHEN ISNULL(ProspectResponse, '') <> '' THEN ProspectResponseAt
            ELSE NULL
        END
WHERE Intent IS NULL;
GO

-- 2) Funnel stage — derive the furthest reached stage from existing step flags.
UPDATE NaukriProspectiveInterviewer
SET FunnelStage =
        CASE
            WHEN OnboardingCompleted = 1                          THEN 'Onboarded'
            WHEN AcknowledgementEmailSent = 1                     THEN 'AcknowledgementSent'
            WHEN ResumeAIAnalysisCompleted = 1                    THEN 'ResumeFitment'
            WHEN EmploymentVerified IS NOT NULL                   THEN 'EmploymentVerified'
            WHEN Intent IS NOT NULL                               THEN 'IntentConfirmed'
            WHEN InvitationEmailSent = 1                          THEN 'InvitationSent'
            WHEN ISNULL(MagName, '') <> ''                        THEN 'MAGMapped'
            WHEN ResumeParseCompleted = 1                         THEN 'Parsed'
            ELSE NULL
        END
WHERE FunnelStage IS NULL;
GO

-- 3) Ticket status — map the old conflated Status into the operational dimension.
UPDATE NaukriProspectiveInterviewer
SET TicketStatus =
        CASE
            WHEN LOWER(ISNULL(Status, '')) IN ('closed', 'onboarded', 'rejected') THEN 'Closed'
            WHEN LOWER(ISNULL(Status, '')) IN ('inprogress', 'verified')          THEN 'InProgress'
            WHEN OnboardingCompleted = 1                                          THEN 'Closed'
            ELSE 'Open'
        END
WHERE TicketStatus IS NULL;
GO

-- 4) Onboarding type — only meaningful for already-onboarded rows.
--    Self-registration wins; otherwise look for an automation actor in the history.
UPDATE np
SET OnboardingType =
        CASE
            WHEN np.SelfRegistrationCompleted = 1 THEN 'SelfOnboarded'
            WHEN EXISTS (
                    SELECT 1 FROM NaukriOnboardingHistory h
                    WHERE h.RecordId = np.Id AND h.ChangedBy = 'AutomationJob'
                 ) THEN 'IntentAutoProcessed'
            ELSE 'ManuallyOnboarded'
        END
FROM NaukriProspectiveInterviewer np
WHERE np.OnboardingType IS NULL
  AND (np.OnboardingCompleted = 1 OR np.SelfRegistrationCompleted = 1);
GO
