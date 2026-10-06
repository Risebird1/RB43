# Voice Agents (Vapi + Plivo)

## Channels

| Channel | Provider | Log table | Eligibility SP (schedule) | Eligibility SP (reschedule) | Timer job |
|---------|----------|-----------|---------------------------|-----------------------------|-----------|
| Legacy voice | Vapi.ai | `VapiCallLogs` | `usp_GetEligibleCandidatesForVapiCalls` | — | `VapiInterviewSchedulingJob` |
| Voice (current) | Plivo CX | `PlivoCXCallLogs` | `usp_GetEligibleCandidatesForPlivoCXCalls` | `usp_GetEligibleCandidatesForPlivoCXReschedule` | `PlivoCXInterviewSchedulingJob`, `PlivoCXRescheduleJob` |
| WhatsApp | Plivo | `PlivoWhatsAppLogs` | `usp_GetEligibleCandidatesForPlivoWhatsApp` | `usp_GetEligibleCandidatesForPlivoWhatsAppReschedule` | `PlivoWhatsAppScheduleJob`, `PlivoWhatsAppRescheduleJob` |

Schedule and reschedule SPs are **mutually exclusive** (rule 4d / mirror rules).

## Code map

| Layer | Vapi | Plivo CX | Plivo WhatsApp |
|-------|------|----------|----------------|
| Service | `VapiService` | `PlivoCXService` | `PlivoWhatsAppService` |
| Repository | `VapiCallRepository` | `PlivoCXCallRepository` | `PlivoWhatsAppRepository` |
| Business | `VapiInterviewService` | (via repository + jobs) | (via repository + jobs) |
| Web UI | `PlivoCXInterviewCallController`, `PlivoInterviewCallController` | `PlivoCXInterviewCallController` | `PlivoWhatsAppInterviewController` |
| Agent instructions | — | `Plivo_*_Agent_Instructions.txt` (repo root) | `Plivo_WhatsApp_*_Agent_Instructions.txt` |

## Plivo CX eligibility (schedule)

`usp_GetEligibleCandidatesForPlivoCXCalls` — all rules must pass:

1. Valid email + phone; not cold (`Status <> 9`); disposition not `uninterested`
2. No future non-cancelled meeting (`InterviewMeetings`, `confirmationStatus <> '2'`)
3. No pending future booking in call logs (`BookingApiSuccess = 1`, future `BookedInterviewDateTime`)
4. No recent valid confirmed interview (15 days, non-reschedule status)
5. Not in reschedule flow (mirror of reschedule SP)
6. `< 2` completed calls unless `PlivoCXDisposition IS NULL`
7. Daily cap: `< 4` schedule calls today (UTC day on `CreatedAt`)
8. 2-hour cooldown since last schedule call
9. 4-day window from first schedule call

Repository passes `@NowIST = DateTime.Now` because `InterviewMeetings.interviewDate` is IST.

**Performance:** use `Risebird.DB/Migrations/Optimize_GetEligibleCandidatesForPlivoCXCalls.sql` (CTE rewrite + indexes).

## Schema joins (critical)

```
Candidate.IRFId          INT  → IRF.id
IRF.IRFId                VARCHAR → InterviewMeetings.IRFId
PlivoCXCallLogs.IRFId    INT  → Candidate.IRFId (not IRF.IRFId)
PlivoCXCallLogs join     RecipientEmail = UserList.EmailId AND IRFId = Candidate.IRFId
Candidate → CandidatesMaster → UserList (email)
```

## Config keys (Functions / Web)

```json
"PlivoCX": {
  "TriggerEndpointUrl": "...",
  "AvailabilityCallerId": "...",
  "OnboardingCallerId": "..."
},
"Vapi": {
  "ApiKey": "...",
  "PhoneNumberId": "...",
  "InterviewBookingAssistantId": "..."
}
```

`HostDetails:baseUrl` — webhook callback base URL.

## Manual testing

- Plivo CX UI: `/PlivoCXInterviewCall`
- Vapi UI: `/VapiInterviewCall` → Setup Assistant → Initiate Call
- Trigger timer manually via Function App HTTP endpoints where exposed

## Data retention

`PurgeOldDataJob` / `usp_PurgeOldData` — default 30 days for Vapi/Plivo log tables (`Purge:VapiPlivoRetentionDays`).

See [Deployment.md](Deployment.md) for deploy order.
