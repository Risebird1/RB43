---
name: risebird-database
description: >-
  Risebird SQL Server patterns — migrations, stored procedures, IRF ID quirks,
  and eligibility query optimization. Use for SP changes, indexes, or schema work.
---

# Risebird Database

## Workflow
1. Edit `Risebird.DB/StoredProcedures/usp_*.sql`
2. Add idempotent `Risebird.DB/Migrations/*.sql` for indexes/columns
3. Deploy migration then SP to Azure SQL

## ID quirks
| Column | Table | Join to |
|--------|-------|---------|
| `IRFId` int | Candidate, PlivoCXCallLogs | `IRF.id` |
| `IRFId` varchar | InterviewMeetings | `IRF.IRFId` |

## Timezones
- `interviewDate` / `@NowIST` → IST
- `PlivoCXCallLogs.CreatedAt` → UTC

## Meeting status
Reschedule-needed: `MeetingStatus IN (1,3,5,9)`, cancelled (`confirmationStatus='2'`), or `RedoInterview=1`.

## Performance pattern
Replace per-row correlated subqueries with grouped CTEs over log/meeting tables.

Details: `Docs/Database.md`
