# Database

## Layout

```
Risebird.DB/
  Tables/           CREATE TABLE + incremental ALTER blocks
  StoredProcedures/ Canonical SP source (deploy to SQL Server)
  Migrations/       Idempotent one-off scripts (indexes, columns)
  Scripts/          PowerShell for Azure Search indexes
  Functions/        SQL functions
  Trigger/          Triggers
```

**Convention:** update `StoredProcedures/*.sql` for logic changes; add `Migrations/*.sql` for index/column deploys that must run once in production.

## ID and join quirks

| Concept | Type | Used in |
|---------|------|---------|
| `IRF.id` | INT PK | `Candidate.IRFId`, `PlivoCXCallLogs.IRFId` |
| `IRF.IRFId` | VARCHAR business id | `InterviewMeetings.IRFId` |
| `Candidate.CandidateMasterId` | INT | → `CandidatesMaster.id` → `UserList.id` → `EmailId` |

Always join `InterviewMeetings` on `irf.IRFId` (varchar), not `c.IRFId` (int).

## Timezones

| Column / param | Timezone |
|----------------|----------|
| `InterviewMeetings.interviewDate` | IST |
| `@NowIST` in eligibility SPs | IST (`DateTime.Now` from C#) |
| `PlivoCXCallLogs.CreatedAt` | UTC (`GETUTCDATE()`) |
| Daily call cap / cooldown | UTC boundaries on `CreatedAt` |

## Interview meeting status codes

| `MeetingStatus` | Meaning |
|-----------------|---------|
| NULL | Scheduled / not evaluated |
| 1 | No Show |
| 3 | Candidate Absent |
| 5 | Tech Issue |
| 9 | Redo |
| 4 | Completed (typical success) |

| `confirmationStatus` | Meaning |
|---------------------|---------|
| `'1'` | Confirmed |
| `'2'` | Cancelled |

Reschedule-needed: `MeetingStatus IN (1,3,5,9)` OR cancelled OR `RedoInterview = 1`.

## Plivo CX indexes (performance)

Deploy: `Migrations/Optimize_GetEligibleCandidatesForPlivoCXCalls.sql` or the slimmer index-only migration if present.

Key indexes:
- `PlivoCXCallLogs (CandidateId, CreatedAt)` — preferred when `CandidateId` populated
- `PlivoCXCallLogs (RecipientEmail, IRFId, CreatedAt)` — fallback join path
- `InterviewMeetings (candidateEmail, IRFId, interviewDate)`
- `InterviewComments (InterviewId)` INCLUDE `MeetingStatus`

## Purge

`usp_PurgeOldData` — trims old rows from webhook debug/checkpoint tables and call logs. Retention configured in Function App (`Purge:VapiPlivoRetentionDays`, `Purge:OpenAIRetentionDays`).

## Deploy checklist (voice eligibility change)

1. Run migration SQL (indexes)
2. Deploy updated SP from `StoredProcedures/`
3. `UPDATE STATISTICS` on touched tables if needed
4. Verify with `EXEC usp_GetEligibleCandidatesForPlivoCXCalls @Count=10, @NowIST=...`
