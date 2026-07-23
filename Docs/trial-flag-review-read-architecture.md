# Trial Flag Review — lazy two-tier read architecture

Branch: `trial-read-architecture`  
Base: deployed Trial Flag Review stack (`23rdjuly-ruma` / production consumer of `usp_GetTrialFlagsForReview`).

## Problem

`usp_GetTrialFlagsForReview` computed every heavy field for every queue row on every page load
(JSON_VALUE over `MergedAIQCDetails`, PaymentSLADetails, CandidateFeedbackDetails, multi-branch
UserList OUTER APPLY). Under concurrent 90-day backfill writers the page timed out:

> Could not load trial flags. The wait operation timed out.

**Mandate:** do **not** raise the SQL command timeout. Architecture must make the timeout irrelevant.

## Part 0 — Diagnosis (run on staging/prod while reproducing)

1. **Blocking vs cost** while the page is open and a backfill runs:

```sql
SELECT r.session_id, r.status, r.wait_type, r.wait_time, r.blocking_session_id,
       r.cpu_time, r.total_elapsed_time, r.reads, r.writes,
       SUBSTRING(t.text, (r.statement_start_offset/2)+1,
         ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
           ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS stmt
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE t.text LIKE '%TrialFlag%' OR t.text LIKE '%usp_GetTrialFlag%';
```

Record `wait_type` / `blocking_session_id`. LCK_* + non-zero blocker ⇒ writer contention; high CPU /
elapsed with NULL blocker ⇒ query cost.

2. **Actual plan** of the old wide proc: expect per-row `JSON_VALUE`, OUTER APPLY scans on
   PaymentSLADetails / QcInterviewAIevaluation by InterviewId.

3. **Indexes:** deploy `Risebird.DB/Indexes/TrialFlagReviewReadIndexes.sql` (InterviewId INCLUDE
   indexes). Evidence: missing indexes historically for PaymentSLADetails / CandidateFeedbackDetails /
   InterviewMeetingsJoinTime / RedoInterviews / RiskCategory / InterviewComments.

## Part 1 — Two-tier lazy read

| Tier | Proc | Purpose |
|------|------|---------|
| List | `usp_GetTrialFlagsForReview_List` | Paginated grid only; Severity/Verdict via RiskCategory unique probe; no JSON / PaymentSLA / CFD |
| Detail | `usp_GetTrialFlagDetail` | Heavy fields for **one** `@TrialFlagId` |
| Counts | cheap `GROUP BY ReviewStatus` | Tab badges |

App flow:

1. `GET /Dashboard/TrialFlagReview` → **shell only** (diagnostics + tab counts, no list query).
2. DataTables `serverSide: true` → `GET /Dashboard/TrialFlagReviewList` (`start`/`length`/`search`/`tab`).
3. Row expand → `GET /Dashboard/TrialFlagReviewDetail?trialFlagId=` (cached on the row client-side).
4. Keep/Remove unchanged; on success refresh that row via List (or full page reload of current page).

## Part 2 — Reader / writer isolation

### Backfill batching

- TrialFlags backfill already caps inserts (`BackfillBatchSize = 25`) with no ambient long transaction.
- RiskCategory IRF backfill processes interviews sequentially without one 90-day SqlTransaction.
- Documented in code comments; do not wrap full windows in a single transaction.

### READ_COMMITTED_SNAPSHOT (RCS) — decision record

| Option | Status |
|--------|--------|
| **Preferred:** `ALTER DATABASE … SET READ_COMMITTED_SNAPSHOT ON` | **DBA-coordinated.** Verify tempdb headroom; enable on staging first; retest whole portal under load. |
| **Fallback if vetoed:** procs already use `SET TRANSACTION ISOLATION LEVEL READ COMMITTED` and rely on RCS when present; without RCS, snapshot hints `WITH (READCOMMITTEDLOCK)` are default — optional `WITH (NOLOCK)` is **not** used (incorrect reads). If RCS is vetoed, schedule off-peak backfills and keep List/Detail cheap. |

**PR decision to fill after DBA reply:** `[ ] RCS enabled on staging` / `[ ] RCS vetoed — fallback accepted`.

**Prove:** open Trial Flag Review while a full 90-day RiskCategory backfill runs; list page must still return &lt; 2s.

## Part 3 — Degradation guard

Repository times List / Detail calls. If duration exceeds:

- List: `SlowQueryWarnSecondsList` = **3s**
- Detail: `SlowQueryWarnSecondsDetail` = **2s**

→ `IApplicationErrorLogger` Warning with duration + parameters. Creep becomes a log line, not the next outage.

Default ADO.NET command timeout is **unchanged** (tripwire).

## Part 4 — Parity

Compare sample `TrialFlagId`s: List fields + Detail fields ≡ old wide proc columns (same values).
Script / checklist: `docs/trial-flag-review-parity.md`.

## Acceptance checklist

- [ ] Shell &lt; 1s; first 25-row page &lt; 2s; detail expand &lt; 1s (cold + during backfill)
- [ ] SQL trace: list loads have **no** `JSON_VALUE` / PaymentSLA reads
- [ ] Tab counts cheap + correct; keep/remove refreshes row
- [ ] RCS decision recorded; page loads during backfill
- [ ] Slow-query warning fires on artificial delay
- [ ] Old wide proc retired (RAISERROR stub); maintainer notes in List/Detail headers
- [ ] Staging + product sign-off before production
