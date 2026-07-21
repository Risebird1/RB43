/* =============================================================================
   InterviewerVideoOff mass-fire — Part 0 diagnosis + Part 3 re-grade validation
   Branch: risk-videooff-fix
   =============================================================================

   ROOT CAUSE (code + UI census; confirm with live DB before production sign-off)
   -----------------------------------------------------------------------------
   PaymentSLADetails.InterviewerVideo is a comma-separated checklist from
   GetPaymentSlaDetails.cshtml / InterviewRateRedo.cshtml:

     | Value                              | Genuine video-off? | Notes |
     |------------------------------------|--------------------|-------|
     | Interviewer Video off              | YES                | Exact allowlist token (P0) |
     | Video Off When Mandatory           | NO                 | Contains "Video Off"; caused LIKE mass-fire |
     | Stretched Video After Candidate Left | NO (other issue) | Do not map to InterviewerVideoOff |
     | Left Call Without Notice           | NO (other issue)   | Do not map to InterviewerVideoOff |
     | NULL / '' / No / N/A / NONE        | Not scored         | Never fires |

   CandidateFeedbackDetails.InterviewerVideo uses the same genuine value
   "Interviewer Video off" (InterviewRateRedo checkbox).

   Legacy usp_AddorUpdatePaymentSLADetails.sql has this COMMENTED OUT:
     --if(@InterviewerVideo like '%Video Off When Mandatory%')
   confirming the original team abandoned that pattern as unreliable.

   FIX: usp_AddUpdateRiskCategory #11 exact-matches UPPER(trim(token)) against
   @InterviewerVideoOffViolations = ('INTERVIEWER VIDEO OFF') via STRING_SPLIT on
   PaymentSLADetails + CandidateFeedbackDetails. LIKE '%Video Off%' removed.

   CIRCUIT BREAKER: DmIvAutoQcBackfillService dry-runs the batch, then aborts if
   any check fires on > MassFireThresholdPct (40%) of > MassFireMinSample (20)
   interviews. Override: ignoreMassFireGuard=1 on RefreshIrfRiskCategory.
*/

-- =============================================================================
-- Part 0.1 — Value census (run on staging/prod; paste TOP results into PR)
-- =============================================================================
SELECT TOP 50
    InterviewerVideo,
    COUNT(*) AS c
FROM dbo.PaymentSLADetails
GROUP BY InterviewerVideo
ORDER BY c DESC;

-- Fixture interviews (RoVe9384 window 2026-07-10..17, IRF SONTJAVADS)
SELECT InterviewId, InterviewerVideo, VideoEnable
FROM dbo.PaymentSLADetails
WHERE InterviewId IN (664087, 663198, 663536, 663053, 659067, 660892);

-- =============================================================================
-- Part 0.2 — Candidate feedback source for the same fixtures
-- =============================================================================
SELECT InterviewId, InterviewerVideo, Rating
FROM dbo.CandidateFeedbackDetails
WHERE InterviewId IN (664087, 663198, 663536, 663053, 659067, 660892);

-- How many PaymentSLA rows contain the false-positive checklist option?
SELECT COUNT(*) AS RowsWithVideoOffWhenMandatory
FROM dbo.PaymentSLADetails
WHERE InterviewerVideo LIKE '%Video Off When Mandatory%';

-- How many have the genuine exact token (alone or in CSV)?
SELECT COUNT(*) AS RowsWithGenuineInterviewerVideoOff
FROM dbo.PaymentSLADetails p
CROSS APPLY STRING_SPLIT(p.InterviewerVideo, ',') s
WHERE UPPER(LTRIM(RTRIM(s.value))) = N'INTERVIEWER VIDEO OFF';

-- Positive-control candidates (genuine violation) — pick one InterviewId for AC#3
SELECT TOP 20
    p.InterviewId,
    p.InterviewerVideo,
    cfd.InterviewerVideo AS CandidateFeedbackVideo
FROM dbo.PaymentSLADetails p
LEFT JOIN dbo.CandidateFeedbackDetails cfd ON cfd.InterviewId = p.InterviewId
CROSS APPLY STRING_SPLIT(p.InterviewerVideo, ',') s
WHERE UPPER(LTRIM(RTRIM(s.value))) = N'INTERVIEWER VIDEO OFF'
ORDER BY p.InterviewId DESC;

-- =============================================================================
-- Part 3 — Before / after re-grade (record both in the PR)
-- Deploy: usp_AddUpdateRiskCategory, usp_GetRiskCategoryBackfillCandidatesByIrf
-- Then: IRF Audit → Refresh risk (90d window). If first run aborts on old logic
-- still deployed, that validates the breaker; after predicate deploy, refresh again.
-- Then: EXEC dbo.usp_SyncTrialFlagsVerdictFromRiskCategory (or rely on Refresh sync).
-- =============================================================================

-- Severity distribution
SELECT Severity, COUNT(*) AS Cnt
FROM dbo.RiskCategory
GROUP BY Severity
ORDER BY Severity;

-- InterviewerVideoOff still present
SELECT COUNT(*) AS VideoOffStillPresent
FROM dbo.RiskCategory
WHERE SeverityBreakdown LIKE '%InterviewerVideoOff%';

-- Fixture interviews after re-grade (expect no InterviewerVideoOff unless genuine token)
SELECT InterviewId, Severity, SeverityBreakdown, Description
FROM dbo.RiskCategory
WHERE InterviewId IN (664087, 663198, 663536, 663053, 659067, 660892, 664645, 664404);

-- Clean-interview regression (AC#6): must have NO RiskCategory row (or empty = deleted)
SELECT im.id AS InterviewId, rc.Severity, rc.SeverityBreakdown, tf.OverallVerdict, tf.Severity AS TrialSeverity
FROM dbo.InterviewMeetings im
LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = im.id
LEFT JOIN dbo.TrialFlags tf ON tf.TriggeringInterviewId = im.id
WHERE im.id IN (664645, 664404);

-- Fixture interviewer drill-down (use AutoRemovalMonitor UI for Recommendation screenshots)
-- IRF #24037 SONTJAVADS, window 2026-07-10..17, interviewers RoVe9384 / RaKu1211
GO
