# AI Interview

## Webhook API

**Endpoint:** `POST /AIInterviewWebhook/interview-scheduled`

**Required body:**

| Field | Type | Notes |
|-------|------|-------|
| `skillChannelId` | string | Maps to `MCQIRF.PortalIRFId` |
| `candidateEmailId` | string | Candidate email |
| `currentStatus` | string | e.g. `scheduled`, `completed`, `cancelled` |

**Optional:** `jobId`, `reason` (cancellation), `requestedBy`

**Auth headers** (when configured in MCQ appsettings):

- `service-account-token` → `RisebirdPortalApi:ServiceAccountToken`
- `x-webhook-key` / `x-functions-key` → `RisebirdPortalApi:WebhookSharedKey`

Aliases accepted: `IRFId`, `CandidateEmailId`, `Status`, `Cancellationreason`.

## Typing speed assessment

Implementation spans Web views, `AIInterviewerService`, OpenAI integration for scoring.

**Key concerns:**
- Anti-cheat validation (paste detection, timing anomalies)
- Scores persisted via repository layer; see `TYPING_*` tables/SPs in DB
- OpenAI used for report/scoring in some flows — log via `OpenAIApiLogs`

**Troubleshooting:** verify browser events fire, API returns WPM, DB column populated on submit.

## Listening comprehension

Separate assessment module — question sets, audio playback, scoring stored in DB. Test with `AI_QUESTION_SETS` flows in staging.

## Public candidate status

AI interview status surfaced on public candidate pages — disposition and session state from MCQ/portal sync.

## Accessibility (Add Report)

Report UI accessibility requirements documented in product tickets; ensure form labels and keyboard nav on add-report views.

## Related config

MCQ host appsettings for webhook tokens; portal `appsettings.json` for OpenAI keys used in assessment scoring.
