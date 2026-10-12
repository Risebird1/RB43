# QC Activity dashboard (`/QCActivity`)

Shows what each Quality Controller did in an IST date range. Every number opens the records behind it.

## Access

| Profile | Sees |
|---|---|
| Admin, SupportAdmin, SeniorHR, SeniorInterviewerManagement, InterviewerManagement | Every QC user, plus the "Quality Controllers only" toggle |
| Quality Controller | Only their own row and records. The server enforces this on `Summary` and `Details` |
| Anyone else | Signed out |

Entry points: **Stats → QC Activity** in the top bar (Admin / SeniorHR / SupportAdmin), and the **QC Activity** tile on the Emergency Dashboard.

## Endpoints

- `GET /QCActivity`: the page
- `GET /QCActivity/Summary?from=yyyy-MM-dd&to=yyyy-MM-dd&onlyQc=true`: per-user rows, backlog and totals (JSON, camelCase)
- `GET /QCActivity/Details?actor=<email>&metric=<key>&from&to`: the drill-down, at most 2000 rows
- `GET /QCActivity/Diagnostics?from&to`: data check, **Admin and SupportAdmin only**, read-only (see [Diagnostics](#diagnostics))

The range defaults to the last 7 days, is capped at 92 days and never ends after today (IST).
Accepted metric keys are listed in `QCActivityMetrics` (`Risebird.Entities/ViewModel/QCActivityDashboardVM.cs`). Any other key returns no rows and runs no query.

## Where each number comes from

All numbers come from `usp_QCActivity_CollectRows`, which writes one row per human action into `#QCActivityRows`. The summary and the drill-down both read those rows, so a cell and its drill-down always agree. Actors `AISystem` and `System` are excluded.

| Column | Source | Actor | Stored time |
|---|---|---|---|
| Reports reviewed / penalised / avg % | `PaymentSLADetailsHistory` (human saves) **and** `ClientCommentsForReport` (every rated save), counted once per interview per user (that user's latest save). For a comment row the deduction comes from the current `PaymentSLADetails` row. Penalised means the latest deduction is above 0. Users whose profile is Client are excluded (clients also rate reports). The drill-down detail shows the rating, e.g. "Shortlist (rated 5/5)" | `CreatedBy` | server clock (assumed UTC) |
| Approved / Not approved | `PaymentSLADetails.IsApproved` = 1 / 2 | `ModifiedBy` | server clock |
| QC forms (red flags) | `Trans_QCFormReport` | `Userid` → `UserList.EmailId` | server clock |
| Trial kept / removed | `TrialFlags.ReviewStatus` | `ReviewedBy` | UTC |
| IRF audit backfills | `IrfAuditRiskBackfillRun` + `AutoQcIrfBackfillRun` | `ExecutedBy` | UTC |
| IRF audit kept / removed | `TransInterviewerAuditLog` `RiskAuditKept` / `RiskAuditRemoval` | `ChangedBy` | UTC |
| IRFs | Distinct IRFs across backfills and keep/remove decisions | | |
| Total Red Marker / Total Interviews | `RedMarkerStatusDetails` with Type `IRF` / `TotalInterview`, Status 1 / 2 / 3 | `CreatedBy` | IST |
| Plivo calls | `CallRecordingData` to interviewers, where `CallSid` is a Plivo UUID and `RecordingSid` is null | `Caller` | server clock |
| Open right now | Pending `PaymentSLADetails`, pending `TrialFlags`, active `TransInterviewer` rows with an `AutoQcVerdict`, `TransInterviewerRedMarker.RedMarkerStatus = 1` | | not date-filtered |

The procedures read optional tables and columns (`TrialFlags`, both backfill tables, `TransInterviewerAuditLog`, `CallRecordingData.RecipientKind` / `HangupCause`) through `sp_executesql` behind `OBJECT_ID` / `COL_LENGTH` checks. If one is missing, its numbers are 0.

**Why reviews need two sources.** `usp_AddorUpdatePaymentSLADetails` writes `PaymentSLADetails` (and so, through `tr_PaymentSLADetails`, `PaymentSLADetailsHistory`) only when `@Rating < 5`. Every rated save (`@Rating > 0`) inserts `ClientCommentsForReport` (`CreatedBy`, `CreatedAt`, `Rating`). Counting history alone showed 0 reviews for QCs who mostly save with rating 5.

## Diagnostics

`usp_QCActivity_Diagnostics(@FromIst, @ToIst)`, exposed as `GET /QCActivity/Diagnostics?from&to` for Admin and SupportAdmin. Read-only.

For every source the dashboard reads it returns, per stored actor value (top 15 per source by rows): the rows in the range, the source total and number of actors, the first and last time (IST), the actor's portal profile and name, and a `status`:

| Status | Meaning |
|---|---|
| Quality Controller | Rows are under a QC: the dashboard should count them |
| Not a QC user | Rows are under a portal user with another profile (hidden while "Quality Controllers only" is on) |
| Actor is not an email | The stored value is a name, an id or similar, so it never matches a user |
| Not a portal user | Looks like an email but no `UserList` row has it (e.g. truncated) |
| System actor (excluded) / Client (excluded) | Skipped by the collector on purpose |
| No rows | The source has nothing in the range: a 0 on the page is real |
| Table missing | Optional table not deployed |

Use it whenever a column shows 0 and you expect activity.

## New data capture (from this deploy onwards)

1. **Who removed an interviewer from a skill channel.** `usp_DeactivateSkillChannelInterviewer` gains two optional parameters, `@removedBy` and `@logType`. When `@removedBy` is set, it writes a `RedMarkerStatusDetails` row with Status 3 and the comment "Removed from skill channel". The Total Red Marker page sends `source=TotalRedMarker` and logs Type `IRF`, which the dashboard counts as TRM Removed. Every other caller logs Type `SkillChannelRemoval`, which no existing page reads. Calls with the old two parameters behave exactly as before.
2. **Plivo talk time, status and hangup cause.** `PlivoVoice/Outbound` now adds `callbackUrl=/PlivoVoice/DialCallback` to `<Dial>`. On `DialAction=hangup`, `DialCallback` calls `usp_UpdateCallRecordingDataFromPlivo`, which only updates an existing `CallSid` row. Duration is only ever raised, and a stored recording URL is never overwritten. The existing `RecordingCallback` is unchanged.

Older periods therefore undercount TRM removals, and Plivo talk time shows "n/a" for calls made before the deploy.

## Deploy order

Each script is idempotent:

1. `Risebird.DB/Tables/CallRecordingData.sql`: adds the `HangupCause` and `EndTime` columns. Every other part of the script is skipped when it already exists.
2. `Risebird.DB/Tables/InterviewerCoachingSession.sql`: the coaching-session table
3. `StoredProcedures/usp_UpdateCallRecordingDataFromPlivo.sql`: needs the step 1 columns
4. `StoredProcedures/usp_DeactivateSkillChannelInterviewer.sql`
5. `StoredProcedures/usp_QCActivity_CollectRows.sql`
6. `StoredProcedures/usp_GetQCActivityDashboardSummary.sql`
7. `StoredProcedures/usp_GetQCActivityDashboardDetails.sql`
8. `StoredProcedures/usp_GetInterviewerQCView.sql`, `StoredProcedures/usp_GetInterviewerQCTimeline.sql`, `StoredProcedures/usp_QCActivity_Diagnostics.sql` and `StoredProcedures/usp_GetInterviewerRiskOverview.sql`
9. `StoredProcedures/usp_GetInterviewerExposure.sql` and `StoredProcedures/usp_SaveInterviewerCoachingSession.sql`: the save procedure needs the step 2 table
10. `Risebird.DB/Indexes/QCActivityPerformanceIndexes.sql`: recommended. It adds date indexes on the source tables, plus the caller and CallSid indexes on `CallRecordingData`, so 30–90 day ranges don't scan whole tables. On Azure SQL the indexes are built online. Run it at a quiet time.
11. Deploy `Risebird.Web`. No new app settings are needed, and nothing changes in the Plivo console.

The collector, the view and the exposure procedures skip optional tables that are missing (coaching sessions, trial flags, backfill runs, audit log), so their numbers are 0 until the table exists.

**Rollback:** redeploy the previous web build. The old build calls `usp_DeactivateSkillChannelInterviewer` with two parameters, which the new version still accepts. The new procedures, columns and indexes can stay in place.

### Live-review fixes (Oct 2026 delta)

No tables or columns change, so no migration. Run, in this order, then deploy `Risebird.Web`:

1. `usp_QCActivity_CollectRows.sql`: reviews also read `ClientCommentsForReport`
2. `usp_GetInterviewerQCView.sql`: it calls the collector; new `@MinInterviews` / `@IncludeOpenFlags`
3. `usp_QCActivity_Diagnostics.sql`: new
4. `usp_GetInterviewerRiskOverview.sql`: 3-interview minimum in the default High-Risk filter

Each fix rolls back on its own by restoring the previous procedure. The exception is the Interviewer QC View: the web build passes `@MinInterviews` and `@IncludeOpenFlags`, which the old procedure does not accept, so roll back that procedure and the web build together.

## Interviewer QC View (`/QCActivity/Interviewers`)

The same QC data turned around: one row per interviewer, so a manager can see everything QC did about an interviewer and who still needs a call. Every allowed viewer, including a Quality Controller, sees all interviewers, because QCs need this view to coach.

- **Entry points:** Stats → Interviewer QC View, the switch at the top of `/QCActivity`, the **Exposure** link on each row of the IRF Audit High-Risk Interviewers tab, and the deep link `/QCActivity/Interviewers?interviewer=<email>`.
- **Controls:** period, interviewer search, **Min. interviews** (1+, 3+ default, 5+, 10+; the server clamps any value to 1–50) and **Include older open flags** (off by default). A hint line under the filters says who is listed. Both options are kept in the URL (`minInterviews`, `includeOpenFlags`).
- **Who is listed:** the top 3,000 by attention score among:
  1. interviewers a QC acted on in the range. A routine no-penalty review, a Payment SLA approval or a QC form with no red flags does not list anyone on its own; a penalty, "not approved", a red-flagged QC form, a trial / IRF audit decision, a Total Red Marker / Total Interviews move, a QC call or a coaching session does.
  2. interviewers with at least *Min. interviews* completed interviews in the range (`InterviewComments.MeetingStatus` 4, 6, 7, 8) **and** a bad interview (`RiskCategory` P0 to P5) or an AI review penalty (`PaymentSLADetailsHistory.CreatedBy = 'AISystem'`) in the range.
  3. only with **Include older open flags**: anyone with an open flag now (a pending trial, an auto-QC verdict on an active channel, unreviewed red markers on an active channel, or Total Interviews under review). Open flags are still shown on every listed row.
- **Work columns:** IRFs, Interviews, **Bad** (P0–P5 completed interviews, with the worst severity) and **Bad %**. Bad and Bad % use the same rule as the IRF Audit High-Risk tab (RiskCategory P0 to P5 on completed interviews). Meetings that store the interviewer's coded name (`InterviewerProfile.NickName`) are mapped to the interviewer.
- **Quality problem (`HasIssue`):** (at least *Min. interviews* completed and 2+ bad or at least 25% bad), or any P0/P1 interview, QC penalty or AI penalty, or a trial / IRF audit removal in the range. **Needs call** = a quality problem and no QC call or coaching session in the range.
- **Attention score:** P0/P1 × 5 + bad × 2 + bad % ÷ 10 (only with enough volume) + penalised × 2 + AI penalised + (trial + audit removals) × 2 + open flags (at most 5), plus 5 when the interviewer needs a call. Lifetime red markers are shown but no longer scored, so old flags no longer outrank current problems.
- **Tiles double as filters:** Interviewers, Quality problem, Need a QC call, Bad interviews, Open flags now, Penalised, Removed or inactive, and Coaching coverage (the share of interviewers with a quality problem who got a call or session). Every column header sorts. The CSV includes Bad interviews, Severe (P0/P1), Worst severity, Bad % and Quality problem.
- **Side panel:**
  - the headline numbers, including bad interviews, bad % and worst severity
  - the coaching brief
  - a per-IRF table: channel owner, on IRF, red markers, TRM status, auto-QC verdict, trial status
  - a dated timeline that merges human QC actions, calls and coaching sessions with system events (AI review penalty, trial flag raised, auto-QC flag or removal), filterable, with recordings and report links inline

Procedures: `usp_GetInterviewerQCView(@FromIst, @ToIst, @Search, @InterviewerEmail, @MinInterviews = 3, @IncludeOpenFlags = 0)` and `usp_GetInterviewerQCTimeline(@InterviewerEmail, @FromIst, @ToIst)`. Both read `usp_QCActivity_CollectRows`, so their numbers match the QC Activity page.

## IRF Audit: High-Risk Interviewers tab

`usp_GetInterviewerRiskOverview`'s default filter (when `@MinRiskRatio` is not NULL) needs at least `@MinVolume` (3, matching `DmIvAutoQcAuditConstants.IrfAuditMinVolume`) completed interviews in the window **and** (pooled bad ratio ≥ `@MinRiskRatio` or any P0). One bad interview out of one is no longer listed as 100% risk. **Show all** passes `@MinRiskRatio = NULL` and lifts every filter. The summary counts and the Excel export use the same procedure, so they follow.

## Coaching: one call with everything

The coaching brief (`usp_GetInterviewerExposure` → `InterviewerCoachingBriefBuilder`) groups every issue by the behaviour to change, not by where it came from. A late join can show up as a risk check, a failed trial factor and a Payment SLA reason. The brief shows it once, under "Join every interview on time", with all three as evidence.

- **Themes:** joining on time, camera and proctoring, redo, report quality, rubric and decision, questions, interview time, candidate experience, setup, conduct, other, plus the internal **Channel standing** group (audit, red markers, Total Interviews, trial status).
- **Say line:** each theme carries a talking point for the call. Risk check codes are translated into plain words; for example `InterviewerLateJoin` becomes "Joined the interview late". `Candidate No Show` is never held against the interviewer. The "Video Off When Mandatory" checklist text is ignored, as `usp_AddUpdateRiskCategory` does.
- **Open** means a flag awaiting a decision. Risk checks and deductions are past events.
- **Email summary:**
  - The coach picks the areas, adds a note and previews the exact email.
  - It is sent from "Risebird Quality Team" with replies going to the coach, logged in `SentMailDetails`, and recorded as a coaching session.
  - Channel standing, severity codes and reviewer names never go out, and every value is HTML-encoded.
  - Each interviewer gets at most one summary per 12 hours. The procedure enforces this atomically, and the slot is released if the send fails.
- **Log coaching session:**
  - Records the channel, the outcome (agreed to change, disputed, call back, no answer, email only), the areas discussed and what was agreed (at least 15 characters, checked on the server).
  - A call the coach made to that interviewer in the previous 3 hours is linked by `CallSid`, so talk time and the recording attach to the session.
- **Where sessions show up:**
  - as metric `Coaching` in the collector, so in the QC Activity Coaching column and drill-down, and in interviewer timelines
  - as a **Coached** badge
  - in Coaching coverage
  - a coached interviewer no longer shows **Needs call**

Endpoints on `QCActivityController`:
- `GET InterviewerList`, `GET InterviewerTimeline`, `GET InterviewerBrief`
- `POST CoachingEmailPreview`, `POST SendCoachingEmail`, `POST LogCoachingSession`. These take a JSON body and require the anti-forgery token in the `RequestVerificationToken` header.

**To verify on staging:**
- Send one summary to a test interviewer address. Check how it renders in the inbox, the reply-to address and the `SentMailDetails` row.
- Place one call, log the session, and confirm the `CallSid` link.

## Known limitations

- The dashboard counts actions, not review quality.
- `usp_AutoApprovePaymentSlaDetails` re-saves rows with `ModifiedBy = CreatedBy`, so an auto-approval is credited to the QC who created the report.
- Sources stored on the server clock are assumed to be UTC. If the App Service clock is set to IST, actions within 5.5 hours of midnight land on the wrong day.
- Only answered Plivo calls are logged (`onCallAnswered` → `/User/makecall`).
- Plivo webhooks are anonymous and not signature-validated. A forged post can only update a row whose CallUUID it already knows.
- The Interviewer QC View and the exposure brief compare `InterviewMeetings.interviewDate` as IST, as the Total Red Marker pages do.
- The Interviewer QC View list builds the whole range in one call (the completed-interview set is built once, set-based). Time it on staging for a 90-day range; an index on `InterviewMeetings (interviewDate) INCLUDE (interviewer)` helps if it is slow.
- The High-Risk tab converts `interviewDate` with +330 minutes and counts only active channels, while the Interviewer QC View compares it as IST and counts every channel. Bad / Bad % can therefore differ for interviews within 5.5 hours of the range edges or on inactive channels.
- `InterviewComments.ReportAcceptedBy` is not counted: it has no timestamp, so it cannot be date-filtered.
- `ClientCommentsForReport.CreatedBy` is `varchar(50)`. A reviewer email longer than 50 characters is stored truncated and does not match the user; Diagnostics shows it as "Not a portal user".
- `usp_DeactivateSkillChannelInterviewer` sets `@IRF = @IRFId` (the integer parameter) instead of `IRF.IRFId`, so the "unassign future meetings" step never matches. This branch leaves that unchanged on purpose. Fixing it changes behaviour and needs product sign-off.

## Verification SQL

Replace the dates and the email. Each count must match its dashboard cell.

```sql
DECLARE @from DATE = '2026-09-24', @to DATE = '2026-09-30', @who VARCHAR(150) = 'qc.user@risebird.io';
DECLARE @fromUtc DATETIME = DATEADD(MINUTE, -330, CAST(@from AS DATETIME)),
        @toUtc   DATETIME = DATEADD(MINUTE, -330, DATEADD(DAY, 1, CAST(@to AS DATETIME)));

SELECT COUNT(DISTINCT Interviewid) FROM PaymentSLADetailsHistory
WHERE CreatedBy = @who AND CreatedOn >= @fromUtc AND CreatedOn < @toUtc;

EXEC usp_GetQCActivityDashboardSummary @FromIst = @from, @ToIst = @to, @OnlyQualityController = 1;
EXEC usp_GetQCActivityDashboardDetails @ActorEmail = @who, @Metric = 'PlivoCall', @FromIst = @from, @ToIst = @to;

-- Rated saves per reviewer (the second review source)
SELECT CreatedBy, COUNT(DISTINCT InterviewId) AS Interviews, MIN(Rating) AS MinRating, MAX(Rating) AS MaxRating
FROM ClientCommentsForReport WHERE CreatedAt >= @fromUtc AND CreatedAt < @toUtc
GROUP BY CreatedBy ORDER BY Interviews DESC;

EXEC usp_QCActivity_Diagnostics @FromIst = @from, @ToIst = @to;
EXEC usp_GetInterviewerQCView @FromIst = @from, @ToIst = @to, @MinInterviews = 3, @IncludeOpenFlags = 0;
```
