# AI Interview Webhook API

How to call the MCQ status webhook used by Risebird (same body shape as Turing `interview-scheduled`).

## Endpoint

| Item | Value |
|------|--------|
| Method | `POST` |
| URL | `https://{your-mcq-host}/AIInterviewWebhook/interview-scheduled` |
| Content-Type | `application/json` |
| Auth | Optional — see [Authentication](#authentication) |

Examples:

- Local: `https://localhost:7252/AIInterviewWebhook/interview-scheduled`
- Prod: `https://{your-mcq-host}/AIInterviewWebhook/interview-scheduled`

## Authentication

Send one of these headers when a token is configured in MCQ `appsettings.json`:

| Header | Config key |
|--------|------------|
| `service-account-token` | `RisebirdPortalApi:ServiceAccountToken` |
| `x-webhook-key` | `RisebirdPortalApi:WebhookSharedKey` |
| `x-functions-key` | same as above |

If both `ServiceAccountToken` and `WebhookSharedKey` are empty, the API accepts requests without a token (dev only).

```http
service-account-token: <your-shared-token>
```

## Request body

### Required fields

| Field | Type | Description |
|-------|------|-------------|
| `skillChannelId` | string | Maps to `MCQIRF.PortalIRFId` |
| `candidateEmailId` | string | Candidate email on the AI Interview share row |
| `currentStatus` | string | Status to store (e.g. `scheduled`, `completed`) |

### Optional fields

| Field | Type | Description |
|-------|------|-------------|
| `jobId` | string | Job / IRF job id from Risebird |
| `reason` | string | Cancellation reason (use for `cancelled`) |
| `requestedBy` | string | Email used when fetching report URLs |

Aliases also accepted: `IRFId` → `skillChannelId`, `CandidateEmailId` → `candidateEmailId`, `Status` → `currentStatus`, `Cancellationreason` → `reason`.

### Example — scheduled

```json
{
  "skillChannelId": "PORTAL-IRF-123",
  "candidateEmailId": "candidate@example.com",
  "currentStatus": "scheduled",
  "jobId": "JOB-456"
}
```

### Example — completed

```json
{
  "skillChannelId": "PORTAL-IRF-123",
  "candidateEmailId": "candidate@example.com",
  "currentStatus": "completed",
  "jobId": "JOB-456"
}
```

### Example — cancelled

```json
{
  "skillChannelId": "PORTAL-IRF-123",
  "candidateEmailId": "candidate@example.com",
  "currentStatus": "cancelled",
  "jobId": "JOB-456",
  "reason": "Candidate withdrew"
}
```

### Typical `currentStatus` values

| Value | Behavior |
|-------|----------|
| `scheduled` | Saves status only |
| `completed` | Saves status + fetches `reportUrl` / `videoUrl` / `transcriptUrl` |
| `noshow` / `dropped` / `cancelled` / `coldcandidate` | Saves status |
| `selected` / `strongly selected` / `marginally selected` | Saves status + fetches report URLs |
| `strongly rejected` / `marginally rejected` | Saves status + fetches report URLs |

## Responses

### Success (HTTP 200)

```json
{
  "status": "OK",
  "success": true,
  "message": "updated",
  "irfId": 76,
  "portalIRFId": "PORTAL-IRF-123",
  "candidateEmailId": "candidate@example.com",
  "currentStatus": "completed",
  "jobId": "JOB-456",
  "reason": null,
  "reportUrl": "https://...",
  "videoUrl": "https://...",
  "transcriptUrl": "https://...",
  "fetchMessage": null
}
```

Notes:

- `message` may be `"no share row updated (schedule first)"` if the candidate was never shared for that IRF.
- Missing `PortalIRFId` match still returns **200** with `"no matching AI Interview skill channel for PortalIRFId"` so callers can clear their queue.

### Unauthorized (HTTP 401)

```json
{
  "status": "ERROR",
  "success": false,
  "message": "unauthorized"
}
```

### Bad request (HTTP 400)

```json
{
  "status": "ERROR",
  "success": false,
  "message": "skillChannelId, candidateEmailId and currentStatus are required"
}
```

## Call examples

### cURL

```bash
curl -X POST "https://{your-mcq-host}/AIInterviewWebhook/interview-scheduled" \
  -H "Content-Type: application/json" \
  -H "service-account-token: YOUR_TOKEN" \
  -d "{
    \"skillChannelId\": \"PORTAL-IRF-123\",
    \"candidateEmailId\": \"candidate@example.com\",
    \"currentStatus\": \"scheduled\",
    \"jobId\": \"JOB-456\"
  }"
```

### C# (HttpClient) — same shape as Risebird notifier

```csharp
var requestBody = new
{
    skillChannelId = data.IRFId,
    candidateEmailId = data.CandidateEmailId,
    currentStatus = data.Status,
    jobId = data.jobId
};

var request = new HttpRequestMessage(
    HttpMethod.Post,
    "https://{your-mcq-host}/AIInterviewWebhook/interview-scheduled")
{
    Headers = { { "service-account-token", "YOUR_TOKEN" } },
    Content = new StringContent(
        JsonConvert.SerializeObject(requestBody),
        Encoding.UTF8,
        "application/json")
};

var response = await client.SendAsync(request);
response.EnsureSuccessStatusCode();
```

### PowerShell

```powershell
$uri = "https://{your-mcq-host}/AIInterviewWebhook/interview-scheduled"
$body = @{
  skillChannelId    = "PORTAL-IRF-123"
  candidateEmailId  = "candidate@example.com"
  currentStatus     = "scheduled"
  jobId             = "JOB-456"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" `
  -Headers @{ "service-account-token" = "YOUR_TOKEN" } `
  -Body $body
```

## Prerequisites

1. Skill channel has `AIInterview = 1` and `PortalIRFId` set to the value you send as `skillChannelId`.
2. Candidate was shared via **Share AI Interview** so an `MCQShareList` row exists (`MCQId = AI-{IRFId}`, `MeetingPlatForm = AIInterview`).
3. For report URLs on completed/selected/rejected: client has `PortalTenantId`, and `RisebirdPortalApi` keys are configured.

## Config (`appsettings.json`)

```json
"RisebirdPortalApi": {
  "BaseUrl": "https://api.portal.risebird.io",
  "FunctionsKey": "...",
  "StatusFunctionsKey": "...",
  "WebhookSharedKey": "",
  "ServiceAccountToken": ""
}
```

Set `ServiceAccountToken` (or `WebhookSharedKey`) in production and send the same value in the request header.
