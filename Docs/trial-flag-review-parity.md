# Trial Flag Review — List+Detail vs retired wide proc parity

## Method

1. On a staging DB that still has a **backup** of the old `usp_GetTrialFlagsForReview` body
   (or restore from git history before the RAISERROR stub), pick ≥20 `TrialFlagId`s spanning
   pending/kept/removed and P0–P5.
2. For each id, capture:
   - Old wide row (`SELECT … WHERE TrialFlagId = @id` from old proc output or a saved snapshot).
   - New List row (`usp_GetTrialFlagsForReview_List` filtered / page containing that id).
   - New Detail row (`usp_GetTrialFlagDetail @TrialFlagId`).
3. Assert field equality (null ≡ empty string where UI treated them the same).

## Field map

| Field | List | Detail | Notes |
|-------|------|--------|-------|
| TrialFlagId, Interviewer*, Irf*, Channel*, Triggering*, Severity, DisplayVerdict, Review*, RiskCategoryLabels/Reasons, IsSuspicious, ChannelOwner | ✓ | (Detail re-emits Severity/Verdict/labels) | List is source for grid |
| Pass bits, scores, Join/Duration minutes | | ✓ | TrialFlags + InterviewComments + CFD |
| SeverityBreakdown, CandidateFeedbackComment | | ✓ | |
| QcMerge*, Payment* | | ✓ | ISJSON-guarded JSON |

## Ordering

List: `SeveritySort ASC` (P0…P5), then `FlaggedAtUtc DESC`, then `TrialFlagId DESC`.
Old proc ordered only by `FlaggedAtUtc DESC` — **intentional change** (AC: P0-first across pages).

## Sign-off

| Sample size | Mismatches | Reviewer | Date |
|-------------|------------|----------|------|
| | | | |
