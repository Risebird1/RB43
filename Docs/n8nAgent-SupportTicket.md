# n8n Workflow — Support Ticket Email Classifier

Replaces the Relay.app "Email classifier" agent (folder *Customer Support Agent*,
template `cmmyfbq4q03fa0qkq9bgx6ljg`), which is now legacy. Relay.app is shutting down;
this is the third workflow migrated to n8n, after
[Interviewer Availability](n8nAgent-InterviewerAvailability.md) and
[Naukri Onboarding](n8nAgent-NaukriOnboarding.md).

- **Workflow JSON:** [`Risebird-n8n-SupportTicket-Email-Classifier.json`](../Risebird-n8n-SupportTicket-Email-Classifier.json) (source of truth — import this into n8n)
- **Workflow name in n8n:** `Support Ticket Email Classifier`
- **Server endpoint:** `POST https://portal.risebird.io/SupportTicket/Webhook`
- **Gmail account:** `cs@risebird.io`
- **Drive parent folder:** `RB Iaas Recruiter Support` (`1ELqUDJy0TyVQ27HCGxI_aafZXkEb8jYa`)

---

## Flow Architecture

```
📥 Email received (Gmail, unread only, categories + internal senders excluded, attachments downloaded)
  │
  ├─ 🔧 Normalize email          — flatten Gmail fields, ISO-8601 UTC date, keep only .pdf/.doc/.docx
  ├─ 🚧 Exclude internal/personal senders — filter on the parsed address
  ├─ 🤖 Classify email           — GPT-5.4, JSON object output, 13 extracted fields
  ├─ 🔧 Parse classification     — robust JSON extraction, intent whitelist, field coercion
  │
  ├─ 🔀 IF attachment_count > 0
  │     ├─ YES → Create folder → Split attachments → Upload attachment → Collect uploads ─┐
  │     └─ NO  ────────────────────────────────────────────────────────────────────────────┤
  │                                                                                        │
  ├─ 🔧 Build webhook payload  ◄──────────────────────────────────────────────────────────┘
  └─ 📤 POST → Risebird webhook
```

Every Code node runs in **Run Once for All Items** mode and pairs items either by array
index (where the node chain is strictly 1:1) or by Gmail `message_id` (where it is not).
This matters because Relay created one run per email, whereas a single Gmail poll in n8n
can emit up to 50 items into one execution.

---

## Node Reference

| Node | Type | Notes |
|------|------|-------|
| Email received | `n8n-nodes-base.gmailTrigger` v1.4 | `readStatus: unread`, `simple: false`, `downloadAttachments: true`, prefix `attachment_` |
| Normalize email | Code | Produces `message_id`, `subject`, `from`, `from_email`, `mail_content`, `truncated_body` (2 700 chars), `received_at`, `folder_name`, `attachment_names`, `attachment_count` |
| Exclude internal/personal senders | Filter v2.3 | `notEndsWith` on `from_email` for 8 domains + `notEmpty` |
| Classify email | `@n8n/n8n-nodes-langchain.openAi` v2.3 | Responses API, `textFormat: json_object`, separate system/user messages, 3 retries |
| Parse classification | Code | Robust JSON extraction, intent whitelist, `N/A`→`null`, 10-char `skill_channel_id` validation |
| Has attachments? | IF v2.2 | `attachment_count > 0` |
| Create folder | `googleDrive` v3 (folder) | `Support - <subject> - <received_at>` under `RB Iaas Recruiter Support`, 3 retries |
| Split attachments | Code | One item per qualifying file, binary re-fetched from the trigger by `message_id`, folder id attached |
| Upload attachment | `googleDrive` v3 | Original filename, `inputDataFieldName: data`, 3 retries |
| Collect uploads | Code | Collapses back to one item per email, pairs each Drive URL to its candidate by name |
| Build webhook payload | Code | Convergence point for both branches; adds `source: n8n` |
| Send to Risebird webhook | `httpRequest` v4.5 | Header `X-Webhook-Secret`, 30 s timeout, 3 retries |

---

## Webhook Payload

```jsonc
{
  "sender_email":               "priya@acmecorp.com",
  "subject":                    "Resumes for Java Developer - QUADRJAVA5",
  "mail_content":               "<full plain-text body>",
  "received_at":                "2026-09-10T04:00:00.000Z",   // ISO-8601 UTC
  "webhook_intent":             "New candidate submission",    // one of the 20 categories
  "job_title":                  "Java Developer",
  "skill_channel_id":           "QUADRJAVA5",                  // exactly 10 alphanumerics, else null
  "primary_specialization":     "Java + Spring Boot",
  "mail_body_intent":           "Sharing two candidate profiles",
  "attachment_job_description": true,
  "attachment_pdf":             true,
  "attachment_resumes":         true,
  "drive_folder_link":          "https://drive.google.com/drive/folders/…",  // null when no attachments
  "resume_drive_urls":          ["…", "…"],                    // JD files excluded
  "resume_filenames":           ["…", "…"],                    // parallel array to resume_drive_urls
  "resume_associations": [
    { "resume_url": "…", "resume_filename": "…", "skill_channel_id": "QUADRJAVA5",
      "job_title": "Java Developer", "candidate_name": "Rajdeep Sharma" }
  ],
  "candidate_schedule_data": [
    { "candidate_name": "Rajdeep Sharma", "candidate_email": null,
      "interview_date": "2026-09-15", "interview_time": "14:00", "timezone": "IST",
      "skill_channel_id": "QUADRJAVA5", "resume_drive_url": "…", "resume_filename": "…" }
  ],
  "summary_mail":               "Two Java profiles submitted.",
  "summary_candidates":         "Rajdeep Sharma, Anita Rao",
  "summary_interview_scores":   null,
  "source":                     "n8n"
}
```

`jd_drive_urls` is also present on the workflow item for debugging but is not part of the
payload contract; the server ignores unknown keys.

### Response

```json
{ "success": true, "ticketId": 1234, "source": "n8n", "pendingResumes": 2,
  "message": "2 resume(s) queued for HR review. Assigned to hr@risebird.io." }
```

---

## Security

- Header `X-Webhook-Secret`, value from `RelayWebhook:Secret` in `appsettings.json`.
- The endpoint accepts `X-Webhook-Secret`, `X-Relay-Secret` **or** `X-N8N-Secret`
  (`AutomationWebhookHelper.IsSecretValid`), so the Relay workflow and the n8n workflow can
  run side by side during the cutover. Leave the secret blank in configuration to disable
  validation entirely.
- The system prompt instructs the model to treat the subject, body and attachment
  filenames as data and to ignore instructions embedded in them.

---

## Behaviour Differences From Relay

| Area | Relay.app | n8n | Why |
|------|-----------|-----|-----|
| Runs per email | One run per email | One execution can carry several emails | Every node keys off `message_id`; nothing uses "the first item". |
| `received_at` | ISO-8601 from Relay | Gmail's RFC 2822 normalized to ISO-8601 UTC in `Normalize email` | The server compares against `UtcNow`. |
| Attachment filenames | Never shown to the model | Listed in the user message | Relay asked the model to build `resume_associations` from attachments it could not see. |
| `resume_url` / `resume_drive_url` | Model was asked to emit them before anything was uploaded | Workflow fills them in after upload, matching candidate name to filename | The model cannot know a URL that does not exist yet. |
| Resume ↔ candidate pairing | Positional | Name match first, then upload order for whatever is left | A mis-ordered upload used to attach the wrong resume to a candidate. |
| Auto-schedule resume URL | Always absent, so the server skipped every candidate | Set only on a real name match, or the unambiguous single-candidate/single-file case | Guessing here books the wrong person's interview. Unmatched candidates still reach HR via the Drive folder. |
| JD files | Counted as resumes | Excluded from `resume_drive_urls` (kept in the folder) | Lets the server recognise a JD-only "New interview request" as a channel-creation request. |
| Drive folder | Created for every email | Created only when a qualifying attachment exists | Most support mail has no attachment; Relay created an empty folder per mail. |
| `resume_associations` key | Sent as `"resume_associations "` — with a trailing space | Sent correctly | The trailing space meant the field never bound to the C# model, so per-resume IRF mapping silently never worked on Relay. |
| Attachment filtering | Loop `break` step | Filtered in `Normalize email` before anything else runs | Non-qualifying files no longer inflate `attachment_count`. |
| Model | `relay.gpt-5.4` | `gpt-5.4` | The `relay.` prefix was a Relay-internal namespace. |

---

## Server-Side Changes For This Migration

| File | Change |
|------|--------|
| `Risebird.Entities/ViewModel/SupportTicketVM.cs` | Added `source` to `RelayEmailWebhookPayload`. |
| `Risebird.Web/Controllers/SupportTicketController.cs` | Webhook now uses `AutomationWebhookHelper` for secret validation, timestamp parsing and platform attribution; sets `HasAutoScheduleCandidates`; falls back to the Drive folder link for a scheduling candidate whose resume URL could not be resolved; returns `source`. |
| `Risebird.Repository/SupportTicketRepository.cs` | `InsertTicketAsync` now passes `JobTitle` and `HasAutoScheduleCandidates` — the stored procedure already accepted both, but neither was being sent, so `job_title` was never persisted. |
| `Risebird.Web/appsettings.json` | Comment updated: `RelayWebhook:Secret` is the Support Ticket secret and now accepts three header names. |
| `Risebird.Web/Views/SupportTicket/Index.cshtml` | Copy no longer names Relay.app. |

No database migration is required.

---

## Setup Checklist

1. Import `Risebird-n8n-SupportTicket-Email-Classifier.json` into n8n.
2. Confirm the **Gmail** credential resolves to `cs@risebird.io`.
3. Confirm the **Google Drive** credential and that the parent folder id still points at
   `RB Iaas Recruiter Support`.
4. Open **Classify email** and confirm the model id in the dropdown — the export pins
   `gpt-5.4`, which must exist on the connected OpenAI account.
5. Confirm the **X-Webhook-Secret** header value matches `RelayWebhook:Secret` in the
   deployed `appsettings.json`.
6. Run once with **Execute Workflow** against a real support email that has two resumes
   attached, and check: one ticket row, two pending resumes, correct candidate-to-resume
   pairing, and a Drive folder containing both files.
7. Activate the n8n workflow, then **deactivate the Relay workflow** — until Relay is off,
   both platforms post to the same endpoint and every email produces two tickets.
