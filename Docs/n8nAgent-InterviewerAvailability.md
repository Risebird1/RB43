# n8n Workflow — Interviewer Email Availability

Replaces the Relay.app agent documented in [RelayAgent-InterviewerAvailability.md](RelayAgent-InterviewerAvailability.md),
which is now legacy. Relay.app is shutting down; this is the first workflow migrated to n8n.

- **Workflow JSON:** [`Risebird-n8n-Interviewer-Availability-Extraction.json`](../Risebird-n8n-Interviewer-Availability-Extraction.json) (source of truth — import this into n8n)
- **Workflow name in n8n:** `Interviewer Availability Extraction`
- **Server endpoint:** `POST https://portal.risebird.io/InterviewerAvailability/Webhook`

---

## Flow Architecture

```
📥 Gmail Trigger (unread only, system senders excluded, attachments downloaded)
  │
  ├─ 🔧 Normalize Email        — flatten Gmail fields, ISO-8601 UTC date, count attachments
  ├─ 📄 Extract Resume Text    — PDF → text (fail-safe: no attachment / non-PDF passes through)
  ├─ 🔧 Build AI Input         — re-attach email fields + resume text
  ├─ 🤖 Extract Availability   — GPT-5.4 Mini, JSON object output, 13 fields
  ├─ 🔧 Parse AI               — parse JSON, apply defaults, resolve sender email
  │
  ├─ 🔀 IF attachment_count > 0
  │     ├─ YES → Split Attachments → Upload to Drive → Collect Resume Links ─┐
  │     └─ NO  ─────────────────────────────────────────────────────────────┤
  │                                                                          │
  ├─ 🔧 Finalize Record  ◄──────────────────────────────────────────────────┘
  │
  ├─ 📤 POST → Risebird webhook   (runs first, so the DB write survives a Sheets outage)
  └─ 📊 Append row → Google Sheet
```

Every Code node runs in **Run Once for All Items** mode and pairs items either by array
index (where the node count is 1:1) or by Gmail `message_id` (where it is not). This matters
because Relay created one run per email, whereas a single Gmail poll in n8n can emit up to
50 items into one execution.

---

## Node Reference

| Node | Type | Notes |
|------|------|-------|
| Gmail Trigger | `n8n-nodes-base.gmailTrigger` | `readStatus: unread`, `simple: false`, `downloadAttachments: true`, prefix `attachment_` |
| Normalize Email | Code | Produces `message_id`, `subject`, `from`, `from_email`, `body`, `body_excerpt`, `received_at`, `attachment_count` |
| Extract Resume Text | `extractFromFile` (pdf) | `onError: continueRegularOutput`, `alwaysOutputData: true` |
| Build AI Input | Code | Adds `resume_text` (capped at 8 000 chars) |
| Extract Availability (AI) | `@n8n/n8n-nodes-langchain.openAi` v2.3 | Responses API, `textFormat: json_object`, separate system/user messages |
| Parse AI | Code | Robust JSON extraction, `N/A` defaults, `sender_email` resolution |
| Has Resume Attachment | IF | `attachment_count > 0` |
| Split Attachments | Code | One item per attachment, binary re-fetched from the trigger by `message_id` |
| Upload Resume to Drive | `googleDrive` v3 | Folder `RB Interviewer Email Availability Internal`, original filename |
| Collect Resume Links | Code | Collapses back to one item per email; first link wins |
| Finalize Record | Code | Sets `resume_link` (or `None`), `has_attachment`, `source: n8n` |
| Send to Risebird Webhook | `httpRequest` v4.5 | Header `X-Webhook-Secret`, 3 retries |
| Add Row to Sheet | `googleSheets` v4.7 | `RB Interviewer Email Availability Relay Internal · Sheet1` |

---

## Webhook Payload

```json
{
  "sender_email": "interviewer@example.com",
  "subject": "Re: Interview availability",
  "mail_content": "...full plain-text body...",
  "received_at": "2026-08-10T09:52:11.000Z",
  "job_title": "Senior Java Developer",
  "name": "Asha Menon",
  "phone": "+919812345678",
  "mag_name": "N/A",
  "irf_id": "N/A",
  "has_login_credentials": "No",
  "slots_per_day": "2",
  "time_frame": "10 AM-2 PM, 6-10 PM",
  "availability_intent": "Available Mon–Wed next week, mornings preferred",
  "mail_body_intent": "Sharing availability for next week",
  "concerns_or_not_interested": "None",
  "resume_fitment": "Strong Fit",
  "resume_link": "https://drive.google.com/file/d/.../view",
  "has_attachment": true,
  "source": "n8n"
}
```

`source` is the only new field versus the Relay contract. When it is absent the server
treats the call as legacy Relay, so old and new workflows can post to the same endpoint.

### Response

```json
{
  "success": true,
  "id": 42,
  "message": "Interviewer availability record created (#42) for interviewer@example.com."
}
```

Stale and system emails return `200` with `{ "success": false, "skipped": true, "reason": "..." }`,
so n8n does not mark the execution as failed.

---

## Security

The endpoint accepts the shared secret via any of these headers:

- `X-Webhook-Secret` — used by this workflow
- `X-Relay-Secret` — legacy, kept working for the not-yet-migrated Relay workflows
- `X-N8N-Secret`

All three are compared against `RelayWebhook:InterviewerAvailabilitySecret` in `appsettings.json`.

---

## Behaviour Differences From Relay

Fixes and deliberate improvements over the Relay agent:

1. **Real resume link in the webhook.** Relay's HTTP step sat in the main thread, above the
   Drive upload, so it always posted `resume_link: "None"` and `has_attachment: false`.
   Both now carry real values.
2. **One sheet row per email.** Relay iterated attachments but wrote a single row. A naive
   n8n port writes one row per attachment; `Collect Resume Links` collapses it back to one.
3. **Correct `Slots Per Day`.** Relay's no-attachment path wrote `has_login_credentials`
   into the `Slots Per Day` column.
4. **Unread-only trigger.** Relay's trigger filter was an `OR` of "is unread" and "not a
   system sender", which matched every read email from a human sender.
5. **Authoritative sender email.** Taken from the Gmail `From` header, with the model's
   extraction as fallback, rather than trusting the model for the primary key.
6. **UTC timestamps.** `received_at` is always ISO-8601 UTC, so the server's 46-hour
   staleness check compares like with like.
7. **Prompt injection guard.** The system prompt instructs the model to treat email and
   resume content strictly as data.

Known limitations:

- **Google Drive links are not machine-readable.** `resume_link` is a Drive share link, whose
  viewer page contains no PDF bytes. Because Relay always posted `"None"`, the server used to
  fall back to the profile resume every time; now that a real link arrives, resume text
  resolution tries `ResumeLink` first and falls back to `ProfileResumeLink` when it yields
  nothing (`ExtractResumeTextAsync`). Without that fallback, sending a real link would have
  blocked auto-close, since a missing fitment verdict gates AG assignment.

- **Resume text extraction is PDF-only.** Relay passed the raw attachment to the model,
  which could read more formats. DOCX resumes yield `resume_fitment: "N/A"`. Add a
  conversion step if that becomes common.
- **`resume_fitment` is forced to `N/A`** whenever no resume text was extracted, rather
  than letting the model guess.

---

## Server-Side Changes For This Migration

| File | Change |
|------|--------|
| `Risebird.Entities/ViewModel/InterviewerEmailAvailabilityVM.cs` | Payload gains an optional `source` field |
| `Risebird.Web/Controllers/InterviewerAvailabilityController.cs` | `IsWebhookSecretValid` (three accepted headers), `ParseWebhookTimestamp` (RFC 2822 + ISO → UTC), `ResolveWebhookSource`, and `ExtractResumeTextAsync` with a candidate-URL fallback |
| `Risebird.Web/Views/InterviewerAvailability/Index.cshtml` | `n8n` source filter option and badge |
| `Risebird.Web/appsettings.json` | Comment documents the accepted secret headers |

No database migration is required — `Source` is a nullable `VARCHAR(50)` with no constraint.
The endpoint stays backwards compatible, so the remaining Relay workflows keep working
against it unchanged.

---

## Setup Checklist

1. Import the workflow JSON into n8n.
2. Re-select the three Google credentials (Gmail, Drive, Sheets) — credential IDs do not
   transfer between instances.
3. **Confirm the model id** on `Extract Availability (AI)`. Relay used `relay.gpt-5.4-mini`;
   the `relay.` prefix was a Relay-internal namespace, so the node is set to `gpt-5.4-mini`.
   Pick the equivalent from the dropdown if that id is not offered.
4. Verify the secret in `Send to Risebird Webhook` matches
   `RelayWebhook:InterviewerAvailabilitySecret`.
5. Run once manually against a test email, then check that the record appears in the
   Availability grid with an `n8n` source badge.
6. Activate the workflow, and only then deactivate the Relay agent.
