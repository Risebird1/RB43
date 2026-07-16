/* =============================================================================
   Trial risk delta-3 — validation / PR evidence queries
   Deploy order:
     1) usp_AddUpdateRiskCategory.sql (#14–#16)
     2) usp_GetTrialFlagsForReview.sql (QcMergeFlagsJson / PaymentReasonValuesJson)
     3) usp_GetTrialFlagBackfillCandidates.sql (pair-based)
   Then: RiskCategory 90-day backfill + TrialFlags backfill.
   ============================================================================= */

-- Before/after severity distribution (paste both into the PR)
SELECT Severity, COUNT(*) AS Cnt
FROM dbo.RiskCategory
GROUP BY Severity
ORDER BY Severity;

-- New P2 check presence
SELECT
    SUM(CASE WHEN SeverityBreakdown LIKE '%LessDurationContent%' THEN 1 ELSE 0 END) AS LessDurationContent,
    SUM(CASE WHEN SeverityBreakdown LIKE '%FewerQuestionsAsked%' THEN 1 ELSE 0 END) AS FewerQuestionsAsked,
    SUM(CASE WHEN SeverityBreakdown LIKE '%ShortRejectComment%' THEN 1 ELSE 0 END) AS ShortRejectComment
FROM dbo.RiskCategory;

-- Phantom-flag regressions (expect 0 badges at render for these shapes):
-- (a) PaymentSLA null reasoning → PaymentReasonValuesJson all-null / empty object
-- (b) MergedAIQCDetails flags entry with category but null/empty flag → CollectMergeFlagBadges skips

-- Pair-based backfill smoke (optional — replace @InterviewerId / @IrfId with the reported miss)
-- EXEC dbo.usp_GetTrialFlagBackfillCandidates @MaxRows = 50, @ActivityWindowDays = 14;

/*
  Part 8 — Removal behavior (code review, no code change):
  - Trial Remove → MeetingService.RemoveOrShortlistInterviewerFromIRF
    → usp_updateInterviewerIRFStatusByIRFid with action='Rejected'
    → UPDATE TransInterviewer SET ClientAction='Rejected', IsActive=0, IsAvailable=0
    → Does NOT DELETE the TransInterviewer row (soft inactivation).
  - Risk-audit / AutoQc Remove → same RemoveOrShortlist + UpdateIRFInterviewerActiveStatus
    + TransInterviewerAuditLog InsertAsync (AutoQcRemoval / risk events).
  - Trial Remove path does NOT currently write TransInterviewerAuditLog by itself;
    MAG/assignment treat IsActive=0 as inactivated (recoverable), not deleted.
*/
GO
