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
--   Parsed                       -> PhoneParsed (if a phone was parsed) else EmailParsed
--   MAGMapped                    -> MAGConfirmed
--   InvitationSent               -> InterestCollection
--   AwaitingResponse             -> InterestCollection
--   IntentCollection             -> InterestCollection   (typo'd legacy spelling)
--   Unsubscribe                  -> NotInterested
--   (IntentConfirmed, EmploymentVerified, ResumeFitment, AcknowledgementSent,
--    Onboarded, Duplicate, NotInterested, Cold) — unchanged.
--
-- Idempotent: safe to re-run; only rows still carrying legacy values are touched.
-- Self-verifying: prints the distribution before and after, plus a reconciliation
-- check (sum of canonical buckets must equal the table total).
-- ============================================================================

SET NOCOUNT ON;

PRINT '--- BEFORE: raw FunnelStage distribution ---';
SELECT ISNULL(NULLIF(LTRIM(RTRIM(FunnelStage)), ''), '(null/blank)') AS FunnelStage_Raw,
       COUNT(*) AS Cnt
FROM NaukriProspectiveInterviewer
GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(FunnelStage)), ''), '(null/blank)')
ORDER BY Cnt DESC;

-- Parsed -> EmailParsed / PhoneParsed (trim-tolerant)
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = CASE WHEN ISNULL(LTRIM(RTRIM(ParsedPhone)), '') <> '' THEN 'PhoneParsed' ELSE 'EmailParsed' END,
    UpdatedAt   = GETUTCDATE()
WHERE LTRIM(RTRIM(FunnelStage)) = 'Parsed';

-- MAGMapped -> MAGConfirmed
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'MAGConfirmed', UpdatedAt = GETUTCDATE()
WHERE LTRIM(RTRIM(FunnelStage)) = 'MAGMapped';

-- InvitationSent / AwaitingResponse / IntentCollection -> InterestCollection
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'InterestCollection', UpdatedAt = GETUTCDATE()
WHERE LTRIM(RTRIM(FunnelStage)) IN ('InvitationSent', 'AwaitingResponse', 'IntentCollection');

-- Unsubscribe -> NotInterested (drop/unsubscribe fold into the Not Interested classification).
-- The unified Intent field and InactivationType still retain 'Unsubscribe' for deactivation logic.
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = 'NotInterested', UpdatedAt = GETUTCDATE()
WHERE LTRIM(RTRIM(FunnelStage)) = 'Unsubscribe';

-- Normalise any stray leading/trailing whitespace on already-canonical values so that
-- exact-match filters/joins behave consistently.
UPDATE NaukriProspectiveInterviewer
SET FunnelStage = LTRIM(RTRIM(FunnelStage))
WHERE FunnelStage <> LTRIM(RTRIM(FunnelStage));

PRINT '--- AFTER: canonical FunnelStage distribution ---';
SELECT dbo.fn_NaukriCanonicalFunnelStage(FunnelStage) AS FunnelStage_Canonical,
       COUNT(*) AS Cnt
FROM NaukriProspectiveInterviewer
GROUP BY dbo.fn_NaukriCanonicalFunnelStage(FunnelStage)
ORDER BY Cnt DESC;

-- Reconciliation: any row whose canonical value is NOT one of the recognised buckets
-- indicates an unexpected legacy value that still needs a mapping rule.
PRINT '--- RECONCILIATION: unrecognised stage values (should be empty) ---';
SELECT dbo.fn_NaukriCanonicalFunnelStage(FunnelStage) AS UnrecognisedStage,
       COUNT(*) AS Cnt
FROM NaukriProspectiveInterviewer
WHERE dbo.fn_NaukriCanonicalFunnelStage(FunnelStage) NOT IN (
        'New','EmailParsed','PhoneParsed','DuplicateChecked','MAGConfirmed',
        'InterestCollection','IntentConfirmed','EmploymentVerified','ResumeFitment',
        'AcknowledgementSent','Onboarded','Duplicate','NotInterested','Cold','NotQualified')
GROUP BY dbo.fn_NaukriCanonicalFunnelStage(FunnelStage)
ORDER BY Cnt DESC;
GO
