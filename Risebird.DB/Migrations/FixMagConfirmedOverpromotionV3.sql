-- ============================================================================
-- Migration: Fix MAGConfirmed over-promotion from the original backfill (V3)
--
-- PROBLEM
--   The Phase-1 backfill derived FunnelStage with the rule
--       WHEN ISNULL(MagName,'') <> '' THEN 'MAGMapped'  (-> MAGConfirmed in V2)
--   But MagName is parsed from the inbound Naukri/Relay email at INGESTION for
--   virtually every record, whereas in the live flow 'MAGConfirmed' is reached only
--   when an operator/automation actually confirms the MAG (ConfirmMag / SelectSkillChannel),
--   which persists ApplicationLink + AssessmentGroupId (+ SelectedIRFId for channels).
--   As a result ~thousands of merely-parsed leads were mis-placed at MAGConfirmed,
--   leaving 'Duplicate Checked' (the wait-for-MAG-confirmation step) empty.
--
-- FIX
--   Re-derive ONLY the records currently parked at canonical 'MAGConfirmed' that carry
--   NO genuine confirmation signal. Records that were really confirmed (ApplicationLink /
--   AssessmentGroupId / SelectedIRFId present) and anything that advanced past MAGConfirmed
--   (InterestCollection, IntentConfirmed, ...) are left untouched.
--
--   Re-derivation (furthest genuine step actually reached, highest first):
--       IsExistingInterviewer = 1               -> Duplicate          (failed the dup check)
--       ResumeParseCompleted  = 1               -> DuplicateChecked   (parsed & cleared; awaiting MAG confirm)
--       ParsedPhone present                     -> PhoneParsed
--       ParsedEmail present                     -> EmailParsed
--       otherwise                               -> New                (ingest-only lead)
--
--   TicketStatus is deliberately NOT changed (these leads were already 'Open' from the
--   original backfill); only the pipeline position is corrected.
--
-- Idempotent: re-running only re-touches rows still sitting at MAGConfirmed without a
-- confirmation signal. Self-verifying: prints before/after distributions.
-- ============================================================================

SET NOCOUNT ON;

PRINT '--- BEFORE: how many MAGConfirmed rows lack a genuine confirmation signal ---';
SELECT
    CASE WHEN ISNULL(ApplicationLink,'') <> '' OR AssessmentGroupId IS NOT NULL OR SelectedIRFId IS NOT NULL
         THEN 'MAGConfirmed (genuine)' ELSE 'MAGConfirmed (over-promoted)' END AS Bucket,
    COUNT(*) AS Cnt
FROM NaukriProspectiveInterviewer
WHERE dbo.fn_NaukriCanonicalFunnelStage(FunnelStage) = 'MAGConfirmed'
GROUP BY CASE WHEN ISNULL(ApplicationLink,'') <> '' OR AssessmentGroupId IS NOT NULL OR SelectedIRFId IS NOT NULL
              THEN 'MAGConfirmed (genuine)' ELSE 'MAGConfirmed (over-promoted)' END;

UPDATE NaukriProspectiveInterviewer
SET FunnelStage =
        CASE
            WHEN IsExistingInterviewer = 1                                 THEN 'Duplicate'
            WHEN ISNULL(ResumeParseCompleted, 0) = 1                       THEN 'DuplicateChecked'
            WHEN ISNULL(LTRIM(RTRIM(ParsedPhone)), '') <> ''              THEN 'PhoneParsed'
            WHEN ISNULL(LTRIM(RTRIM(ParsedEmail)), '') <> ''              THEN 'EmailParsed'
            ELSE 'New'
        END,
    UpdatedAt = GETUTCDATE()
WHERE dbo.fn_NaukriCanonicalFunnelStage(FunnelStage) = 'MAGConfirmed'
  AND ISNULL(ApplicationLink, '') = ''
  AND AssessmentGroupId IS NULL
  AND SelectedIRFId IS NULL;

PRINT '--- AFTER: canonical FunnelStage distribution ---';
SELECT dbo.fn_NaukriCanonicalFunnelStage(FunnelStage) AS FunnelStage_Canonical,
       COUNT(*) AS Cnt
FROM NaukriProspectiveInterviewer
GROUP BY dbo.fn_NaukriCanonicalFunnelStage(FunnelStage)
ORDER BY Cnt DESC;
GO
