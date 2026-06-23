-- ============================================================================
-- Migration: NaukriOnboarding funnel-stage revamp (V2)
--
-- Aligns existing FunnelStage values with the simplified, linear model from the
-- operator screenshot. The complementary "reached/not-reached" milestone filter
-- is being scrapped; this only renames/reorders the canonical stage values.
--
-- New happy-path values (monotonic):
--   EmailParsed -> PhoneParsed -> DuplicateChecked -> MAGConfirmed ->
--   InterestCollection -> IntentConfirmed -> EmploymentVerified ->
--   ResumeFitment -> AcknowledgementSent -> Onboarded
-- Terminal exits (rank 0): Duplicate, NotInterested, Cold, NotQualified.
--
-- Old -> New mapping:
--   Parsed          -> PhoneParsed (if a phone was parsed) else EmailParsed
--   MAGMapped       -> MAGConfirmed
--   InvitationSent  -> InterestCollection
--   AwaitingResponse-> InterestCollection
--   Unsubscribe     -> NotInterested   (drop/unsubscribe now classify as Not Interested)
--   (IntentConfirmed, EmploymentVerified, ResumeFitment, AcknowledgementSent,
--    Onboarded, Duplicate, NotInterested, Cold) — unchanged.
--
-- Idempotent: safe to re-run; only rows still carrying legacy values are touched.
-- ============================================================================

SET NOCOUNT ON;

-- Parsed -> EmailParsed / PhoneParsed
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = CASE WHEN ISNULL(LTRIM(RTRIM(ParsedPhone)), '') <> '' THEN 'PhoneParsed' ELSE 'EmailParsed' END,
    UpdatedAt   = GETUTCDATE()
WHERE FunnelStage = 'Parsed';
GO

-- MAGMapped -> MAGConfirmed
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'MAGConfirmed', UpdatedAt = GETUTCDATE()
WHERE FunnelStage = 'MAGMapped';
GO

-- InvitationSent / AwaitingResponse -> InterestCollection
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'InterestCollection', UpdatedAt = GETUTCDATE()
WHERE FunnelStage IN ('InvitationSent', 'AwaitingResponse');
GO

-- Unsubscribe -> NotInterested (drop/unsubscribe fold into the Not Interested classification).
-- The unified Intent field and InactivationType still retain 'Unsubscribe' for deactivation logic.
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'NotInterested', UpdatedAt = GETUTCDATE()
WHERE FunnelStage = 'Unsubscribe';
GO
