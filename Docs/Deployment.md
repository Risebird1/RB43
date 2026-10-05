# Deployment

## General order

1. **Database** — migrations → stored procedures → verify
2. **Azure Search indexes** — PS scripts (if schema changed)
3. **App settings** — Functions + Web + Services
4. **Application deploy** — Web, Functions, Services
5. **Full crawl** — search indexes (if new/changed)
6. **Smoke test** — manual call or timer HTTP trigger

## Voice agent deploy (Plivo CX)

1. Deploy SQL:
   - `Tables/PlivoCXCallLogs.sql` (if new env)
   - `StoredProcedures/usp_GetEligibleCandidatesForPlivoCXCalls.sql`
   - `StoredProcedures/usp_GetEligibleCandidatesForPlivoCXReschedule.sql`
   - `Migrations/Optimize_GetEligibleCandidatesForPlivoCXCalls.sql` (indexes + optimized SP)
2. Set Function App config:
   - `ConnectionString:DBConnectionString`
   - `PlivoCX:TriggerEndpointUrl`, caller IDs
   - `HostDetails:baseUrl`
3. Deploy `Risebird.AzureTimerFunctions`
4. Test: `EXEC usp_GetEligibleCandidatesForPlivoCXCalls @Count=5`
5. Hit timer HTTP endpoint or wait for schedule

## Voice agent deploy (Vapi — legacy)

1. `Migrations/Add_CandidateMasterId_To_Candidate.sql` (if needed)
2. `Migrations/Add_IRFId_To_VapiCallLogs.sql`
3. `StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql`
4. Register in `Program.cs`: `IVapiService`, `IVapiCallRepository`, `IVapiInterviewService`
5. Config: `Vapi:ApiKey`, `Vapi:PhoneNumberId`, `Vapi:InterviewBookingAssistantId`

## Azure Search deploy

1. Deploy batch SPs to SQL
2. Run appropriate `Risebird.DB/Scripts/Create-*Index.ps1`
3. Enable listing in appsettings (`Enabled: true`)
4. POST FullCrawl on Function App

## Pre-deploy SQL checks

```sql
-- CandidateMasterId present
SELECT 1 FROM sys.columns
WHERE object_id = OBJECT_ID('Candidate') AND name = 'CandidateMasterId';

-- PlivoCXCallLogs exists
SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PlivoCXCallLogs';
```

## Timer functions (Functions project)

| Function | Purpose |
|----------|---------|
| `PlivoCXInterviewSchedulingJob` | Outbound schedule calls |
| `PlivoCXRescheduleJob` | Reschedule calls |
| `PlivoWhatsAppScheduleJob` | WhatsApp schedule |
| `CallCatalogueIndexSearchFullCrawl` | Search index crawl |
| `PurgeOldDataJob` | Log retention |

## Testing checklist

- [ ] Eligibility SP returns rows in staging
- [ ] Single manual outbound call completes + webhook
- [ ] `InterviewMeetings` row created after booking
- [ ] Candidate excluded on second timer run (cooldown/cap rules)
- [ ] Search listing loads with filters (if index deploy)
