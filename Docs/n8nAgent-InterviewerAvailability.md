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
  ├─ 🤖 Extract Availability   — Information Extractor (GPT-5.4 Mini), 13 schema-validated fields
  ├─ 🔧 Parse AI               — flatten `output`, MAG from subject, defaults, sender email
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
| Normalize Email | Code | Produces `message_id`, `subject`, `from`, `from_email`, `body`, `body_excerpt`, `received_at`, `attachment_count`. `body_excerpt` is the sender's reply with the quoted original stripped; `body` keeps the whole thing |
| Extract Resume Text | `extractFromFile` (pdf) | `onError: continueRegularOutput`, `alwaysOutputData: true` |
| Build AI Input | Code | Adds `resume_text` (capped at 8 000 chars) |
| Extract Availability (AI) | `@n8n/n8n-nodes-langchain.informationExtractor` v1.2 | Manual JSON schema, 13 fields, `systemPromptTemplate`, `onError: continueRegularOutput` |
| GPT-5.4 mini | `@n8n/n8n-nodes-langchain.lmChatOpenAi` v1.3 | Model sub-node, wired via `ai_languageModel` |
| Parse AI | Code | Reads `output`, derives `mag_name` from the subject, `N/A` defaults, `sender_email` resolution, sets `ai_parse_failed` |
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
  "source": "n8n",
  "ai_parse_failed": false
}
```

`source` and `ai_parse_failed` are the only new fields versus the Relay contract. When
`source` is absent the server treats the call as legacy Relay, so old and new workflows can
post to the same endpoint.

`ai_parse_failed` is `true` when the Information Extractor returned nothing, in which case
every field above is the placeholder `"N/A"`. The server then logs an `AIExtractionFailed`
history entry and skips the intent and auto-close decisions instead of reading meaning into
placeholder text. Without this flag a total extraction failure is indistinguishable from an
email that genuinely said nothing — which is exactly how the September 2026 outage went
unnoticed.

### Response

The response reports the decisions, not just the insert, so the n8n execution log shows what
the server actually concluded:

```json
{
  "success": true,
  "id": 46320,
  "source": "n8n",
  "intent": "Available",
  "autoCloseTriggered": true,
  "message": "Interviewer availability record created (#46320) for pratik.interviewer@gmail.com. Intent: Available; auto-close pipeline started (final status depends on the resume-fitment and assessment-group gates)."
}
```

`autoCloseTriggered` means the pipeline *started*. It runs fire-and-forget, so the final
status still depends on the gates in `TryAutoCloseAsync`.

When `ai_parse_failed` is `true` the server returns `aiParseFailed: true` instead, and makes
no intent or auto-close decision.

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
| `Risebird.Web/Views/InterviewerAvailability/Index.cshtml` | `n8n` source filter option and badge; MAG is only printed under the job title when it differs from it |
| `Risebird.Web/appsettings.json` | Comment documents the accepted secret headers |

No database migration is required — `Source` is a nullable `VARCHAR(50)` with no constraint.
The endpoint stays backwards compatible, so the remaining Relay workflows keep working
against it unchanged.

---

## Fix — 14 Sep 2026: every extracted field arriving as "N/A"

**Symptom.** On `/InterviewerAvailability`, Name, Job Title, MAG and Intent all showed
`N/A` for every `n8n` record, and nothing auto-closed — 21 703 of 39 181 records sat Open.
Email, Phone, Company and the RB badge looked fine because those come from the interviewer
profile join, not from the payload.

**Root cause.** The extraction step used the raw `@n8n/n8n-nodes-langchain.openAi` node and
a hand-written parser in `Parse AI` that had to guess which envelope the response came back
in (`output_text` / `output[]` / `message.content` / `content` / `text`). When the shape did
not match any of those guesses, `toObject()` returned `{}` and the `FIELDS` loop wrote the
literal string `'N/A'` into all 13 fields. That placeholder is indistinguishable from a
genuine "not found", so the failure was silent.

Both reported symptoms follow from that one cause:

- `MapEmailToIntent('N/A', 'N/A', 'None')` → `null`, so no unified Intent badge. The two
  `N/A`s visible in the Intent column are `availability_intent` and `mail_body_intent`
  rendering, because `hasIntentSummary` only tests for non-empty.
- `IsFavorableAvailabilityIntent('N/A', 'N/A', 'None')` → `false`, so `TryAutoCloseAsync`
  was never called and every record stayed Open.

**Three further defects found while fixing it:**

1. *`mag_name` was defined wrongly in the prompt* — as "the magazine name, project name, or
   publication". MAG here is **Master Assessment Group**. `ResolvedAssessmentGroupId` is
   resolved by matching `MagName` against `MasterAssessmentGroup.MAGName` (or
   `AssessmentGroup.AssessmentGroup`), and auto-close cannot assign an AG without it — so
   auto-close would still have failed even after extraction was repaired. `Parse AI` now
   reads the labelled values out of the subject line — the same approach the Naukri workflow
   already uses — and only falls back to the model:

   ```
   Re: Risebird - Interviewers Availability Check -MagName : Python Spark - IRFID : SYNEPYTPY8- ...
                                                             ^^^^^^^^^^^^          ^^^^^^^^^^
   ```

   `mag_name` and `irf_id` both come from there, with the body (which repeats both labels) as
   a second fallback. The older `check for skills based on: {MAG} Risebird Interviews`
   campaign subject is still matched too. `job_title` falls back to `mag_name`, because a
   one-word reply like "Interested" names no role and the MAG *is* the skill the campaign
   went out for; the grid only prints the MAG under the job title when the two differ.
2. *Negation inverted the favorability test.* Every positive keyword is a substring of its
   own negation: `"not available"` contains `"available"`, `"not interested"` contains
   `"interested"`. With extraction broken this never fired, but the moment it was fixed a
   declining interviewer would have been marked Available and auto-closed.
   `NegativeAvailabilityPhrases` is now checked first and is shared with `MapEmailToIntent`.
3. *A failed extraction was unreportable.* `Parse AI` already computed `ai_parse_failed`,
   but it was never included in the webhook body, so the server had no way to know. It is
   now sent, logged as history, and suppresses the intent/auto-close decisions.
4. *The outbound boilerplate was being fed back to the model.* The availability mail tells
   the panel to `reply with "Not Interested" or "Unsubscribe"`, and every reply quotes it.
   `MapEmailToIntent` runs `IsUnsubscribeText` over the model's own output fields, so a model
   that echoed that line into `mail_body_intent` would resolve to `Unsubscribe` — which runs
   `DeactivateInterviewerOnUnsubscribeAsync` and pulls the interviewer out of circulation
   system-wide. `Normalize Email` now strips the quoted original before the model sees it, so
   only what the sender actually wrote is classified. `mail_content` still stores the full
   body.

**Fix.** `Extract Availability (AI)` is now an `informationExtractor` with a declared JSON
schema plus a `lmChatOpenAi` sub-node — the pattern the Naukri workflow has been running
successfully. The extractor returns schema-validated fields under `output`, so there is no
envelope left to guess at.

**Re-import required:** this changes the workflow JSON, so import it into n8n again.

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
