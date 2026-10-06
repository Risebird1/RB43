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

The range defaults to the last 7 days, is capped at 92 days and never ends after today (IST).
Accepted metric keys are listed in `QCActivityMetrics` (`Risebird.Entities/ViewModel/QCActivityDashboardVM.cs`). Any other key returns no rows and runs no query.

## Where each number comes from

All numbers come from `usp_QCActivity_CollectRows`, which writes one row per human action into `#QCActivityRows`. The summary and the drill-down both read those rows, so a cell and its drill-down always agree. Actors `AISystem` and `System` are excluded.

| Column | Source | Actor | Stored time |
|---|---|---|---|
| Reports reviewed / penalised / avg % | `PaymentSLADetailsHistory`, counted once per interview per user (that user's latest save). Penalised means the latest deduction is above 0 | `CreatedBy` | server clock (assumed UTC) |
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
8. `StoredProcedures/usp_GetInterviewerQCView.sql` and `StoredProcedures/usp_GetInterviewerQCTimeline.sql`
9. `StoredProcedures/usp_GetInterviewerExposure.sql` and `StoredProcedures/usp_SaveInterviewerCoachingSession.sql`: the save procedure needs the step 2 table
10. `Risebird.DB/Indexes/QCActivityPerformanceIndexes.sql`: recommended. It adds date indexes on the source tables, plus the caller and CallSid indexes on `CallRecordingData`, so 30–90 day ranges don't scan whole tables. On Azure SQL the indexes are built online. Run it at a quiet time.
11. Deploy `Risebird.Web`. No new app settings are needed, and nothing changes in the Plivo console.

The collector, the view and the exposure procedures skip optional tables that are missing (coaching sessions, trial flags, backfill runs, audit log), so their numbers are 0 until the table exists.

**Rollback:** redeploy the previous web build. The old build calls `usp_DeactivateSkillChannelInterviewer` with two parameters, which the new version still accepts. The new procedures, columns and indexes can stay in place.

## Interviewer QC View (`/QCActivity/Interviewers`)

The same QC data turned around: one row per interviewer, so a manager can see everything QC did about an interviewer and who still needs a call. Every allowed viewer, including a Quality Controller, sees all interviewers, because QCs need this view to coach.

- **Entry points:** Stats → Interviewer QC View, the switch at the top of `/QCActivity`, the **Exposure** link on each row of the IRF Audit High-Risk Interviewers tab, and the deep link `/QCActivity/Interviewers?interviewer=<email>`.
- **Who is listed:** the top 3,000 by attention score among:
  - interviewers a QC action, call or coaching session touched in the range
  - interviewers the AI review (`PaymentSLADetailsHistory.CreatedBy = 'AISystem'`) penalised in the range
  - anyone with an open flag now: a pending trial, an auto-QC verdict on an active channel, unreviewed red markers on an active channel, or Total Interviews under review
- **Attention score:** penalised × 2 + AI penalised + open flags × 3 + red markers + (trial + audit removals) × 2, plus 5 when the interviewer needs a call. **Needs call** means there was an issue in the range or an open flag, and no QC call or coaching session in the range.
- **Tiles double as filters:** Interviewers, Need a QC call, Open flags now, Penalised, Removed or inactive, and Coaching coverage (the share of interviewers with an issue who got a call or session). Every column header sorts.
- **Side panel:**
  - the headline numbers
  - the coaching brief
  - a per-IRF table: channel owner, on IRF, red markers, TRM status, auto-QC verdict, trial status
  - a dated timeline that merges human QC actions, calls and coaching sessions with system events (AI review penalty, trial flag raised, auto-QC flag or removal), filterable, with recordings and report links inline

Procedures: `usp_GetInterviewerQCView(@FromIst, @ToIst, @Search, @InterviewerEmail)` and `usp_GetInterviewerQCTimeline(@InterviewerEmail, @FromIst, @ToIst)`. Both read `usp_QCActivity_CollectRows`, so their numbers match the QC Activity page.

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
- The Interviewer QC View list builds the whole range in one call. Time it on staging for a 90-day range; an index on `InterviewMeetings (interviewer, interviewDate)` helps if it is slow.
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
```
