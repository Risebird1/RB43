# n8n Workflow — Naukri Prospective Interviewer Onboarding (LinkedIn Verified)

Replaces the Relay.app agent documented in
[`Relay_NaukriOnboarding_WorkflowGuide.txt`](../Relay_NaukriOnboarding_WorkflowGuide.txt) and its
LinkedIn variant [`Relay_NaukriOnboarding_LinkedInVerified_WorkflowGuide.txt`](../Relay_NaukriOnboarding_LinkedInVerified_WorkflowGuide.txt),
both now legacy. Second workflow migrated off Relay.app, after
[Interviewer Email Availability](n8nAgent-InterviewerAvailability.md).

- **Workflow JSON:** [`Risebird-n8n-Naukri-Onboarding.json`](../Risebird-n8n-Naukri-Onboarding.json) (source of truth — import this into n8n)
- **Workflow name in n8n:** `Naukri Prospective Interviewer Onboarding — LinkedIn Verified`
- **Server endpoint:** `POST https://portal.risebird.io/NaukriOnboarding/Webhook`
- **Gmail account:** `neha_dubey@risebird.io`

---

## Flow Architecture

```
📥 Gmail Trigger (unread, from:naukri.com, subject contains "Online Interviewer", attachments downloaded)
  │
  ├─ 🔧 Normalize Email             — strip HTML → body, ISO-8601 UTC date, count attachments
  ├─ 🔍 Only Naukri Interviewer Emails — guard filter on the normalized fields
  ├─ 📄 Extract Resume Text         — PDF → text (fail-safe: no attachment / non-PDF passes through)
  ├─ 🔧 Build AI Input              — re-attach email fields + resume text
  ├─ 🤖 Extract Candidate Data      — GPT-5.4 Mini information extractor, 12 fields
  ├─ 🔧 Merge Candidate             — flatten, derive mag_name + experience_years from the subject
  │
  ├─ 🔀 IF experience_years >= 8    — false branch dropped (Relay's "STOP_IF < 8")
  │     │
  │     ├─ 🔧 Prepare LinkedIn Query  — name + role + location, site:linkedin.com/in
  │     ├─ 🌐 Google Search (LinkedIn) — SerpAPI (continues on failure)
  │     ├─ 🔧 Prepare LinkedIn Input   — render hits as text, recover the record
  │     ├─ 🤖 Verify LinkedIn Match    — GPT-5.4 Mini verdict (YES/NO, confidence, url, reason)
  │     ├─ 🔧 Merge LinkedIn Verdict   — attach verdict; missing verdict ⇒ explicit NO / 0
  │     │
  │     ├─ 🔀 IF attachment_count > 0
  │     │     ├─ YES → Split Attachments → Upload to Drive → Share (link-read) → Collect Resume Links ─┐
  │     │     └─ NO  ──────────────────────────────────────────────────────────────────────────────────┤
  │     │                                                                                              │
  │     ├─ 🔧 Finalize Record  ◄────────────────────────────────────────────────────────────────────────┘
  │     └─ 📤 POST → Risebird webhook
```

Every Code node runs in **Run Once for All Items** mode and pairs items either by array index
(where the node count is 1:1) or by Gmail `message_id` (where it is not). This matters because
Relay created one run per email, whereas a single Gmail poll in n8n can emit many items into one
execution.

---

## Node Reference

| Node | Type | Notes |
|------|------|-------|
| Gmail Trigger | `gmailTrigger` v1.4 | `q: from:naukri.com subject:"Online Interviewer"`, `readStatus: unread`, `simple: false`, `downloadAttachments: true` |
| Normalize Email | Code | Produces `message_id`, `subject`, `from`, `from_email`, `body_excerpt`, `received_at`, `attachment_count` |
| Only Naukri Interviewer Emails | Filter v2.2 | `from_email` contains `@naukri.com` **and** `subject` contains `Online Interviewer` |
| Extract Resume Text | `extractFromFile` (pdf) | `onError: continueRegularOutput`, `alwaysOutputData: true` |
| Build AI Input | Code | Adds `resume_text` (capped at 8 000 chars) |
| Extract Candidate Data | `informationExtractor` v1.2 | Manual JSON Schema using the **webhook's own field names**; nothing required |
| GPT-5.4 mini | `lmChatOpenAi` v1.3 | Shared by both extractor nodes, `reasoningEffort: low` |
| Merge Candidate | Code | Flattens `output`, derives `mag_name`/`experience_years` from the subject, blanks a `@naukri.com` candidate email |
| Experience >= 8 years | IF v2.2 | Gate on `experience_years_num`; false branch intentionally unconnected |
| Prepare LinkedIn Query | Code | Builds the search string and carries the record past the HTTP node |
| Google Search (LinkedIn) | `httpRequest` v4.5 | SerpAPI, `engine=google`, `num=10`, continues on error, 3 retries |
| Prepare LinkedIn Input | Code | Renders `organic_results` as text; empty ⇒ `NO SEARCH RESULTS AVAILABLE.` |
| Verify LinkedIn Match | `informationExtractor` v1.2 | Relay's full verification prompt; continues on error |
| Merge LinkedIn Verdict | Code | Normalizes to `YES`/`NO`, clamps confidence to 0–100, always supplies a reason |
| Has Resume Attachment | IF v2.2 | `attachment_count > 0` |
| Split Attachments | Code | One item per attachment, binary re-fetched from the trigger by `message_id`; names the file `{candidate_name} - {mag_name} - Resume{ext}` |
| Upload Resume to Drive | `googleDrive` v3 | Folder `RB Naukri Prospective Interviewers`, 3 retries |
| Share Resume File | `googleDrive` v3 (share) | `role: reader`, `type: anyone` — see [Resume link and Drive sharing](#resume-link-and-drive-sharing) |
| Collect Resume Links | Code | Collapses back to one item per email; first link wins |
| Finalize Record | Code | Sets `resume_link` (or `None`), `has_attachment`, `source: n8n` |
| Send to Risebird Webhook | `httpRequest` v4.5 | Header `X-Webhook-Secret`, 3 retries, 30 s timeout |

---

## Webhook Payload

Flat snake_case, deserialized into `RelayNaukriPayload`:

```json
{
  "sender_email": "milanjain902.gmail@naukri.com",
  "subject": "NVite - Naukri.com -Risebird Online Interviewer - Managed File Transfer Expert, ...",
  "mail_content": "...plain-text body recovered from the HTML...",
  "received_at": "2026-08-07T11:40:39.000Z",
  "candidate_name": "Milan Jain",
  "location": "Bengaluru",
  "past_experience": "Middleware Consultant at Michael Page",
  "notice_period": "2 Months",
  "education": "B.Tech / B.E. at MIET, Uttar Pradesh Technical University",
  "key_skills": "Github, Azure CLI, Terraform, ...",
  "current_role": "Principal Consultant at Infosys",
  "experience_years": "17",
  "salary": "27 Lacs",
  "mag_name": "Managed File Transfer Expert",
  "candidate_email": "milanjain902@gmail.com",
  "candidate_phone": "+919876543210",
  "resume_link": "https://drive.google.com/file/d/.../view",
  "has_attachment": true,
  "linkedin_url": "https://www.linkedin.com/in/milanjain/",
  "linkedin_match": "YES",
  "linkedin_confidence": 92,
  "linkedin_reason": "Same employer (Infosys) and city (Bengaluru).",
  "source": "n8n"
}
```

`source` names the **automation platform** and is used only for history attribution. It is
deliberately distinct from the prospect's `Source` column, which stays `Naukri` (the recruitment
channel) no matter which platform ran the workflow.

`linkedin_confidence` is load-bearing: when Signzy employment verification fails, the server
accepts the prospect as employed if a LinkedIn URL is present with confidence ≥ 80. A missing or
failed verdict therefore degrades to `NO` / `0`, never to a value that could trigger a false
acceptance.

---

## Security

The endpoint accepts the shared secret in any of three headers, so Relay and n8n can post side by
side while the remaining workflows are migrated:

| Header | Used by |
|--------|---------|
| `X-Webhook-Secret` | n8n (this workflow) |
| `X-Relay-Secret` | Relay.app (legacy) |
| `X-N8N-Secret` | accepted alternative |

Configured under `RelayWebhook:NaukriOnboardingSecret`. Blank configuration disables validation.

---

## Resume Link and Drive Sharing

`/NaukriOnboarding/Webhook` downloads `resume_link`, converts a Drive `/file/d/{id}/` URL to
`uc?export=download&id={id}`, and copies the bytes into Azure blob storage — the record's stored
`ResumeLink` becomes that blob URL. It fetches the URL **unauthenticated**, so a Drive file that is
not link-readable returns a sign-in page instead of the PDF. The server detects the HTML and skips
the copy, leaving a Drive URL it cannot read.

`Share Resume File` grants `role: reader` / `type: anyone` on the uploaded file to close that gap.
The trade-off is that each resume becomes readable by anyone holding the link.

If that is unacceptable, delete the `Share Resume File` node and connect `Upload Resume to Drive`
straight to `Collect Resume Links`. The prospect still saves and the Drive link is still stored;
what is lost is the Azure blob copy and the server-side resume-parse fallback. That fallback only
matters when the AI failed to extract `candidate_email`/`candidate_phone` — when it succeeds, the
server marks resume parsing complete and never needs to read the file.

---

## Behaviour Differences from Relay

| Area | Relay | n8n |
|------|-------|-----|
| Email body | `trigger.Body` | Naukri mails are HTML-only, so the body is recovered by stripping the HTML — reading `json.text` yields nothing |
| Mail with no attachment | The HTTP call sat inside the attachment iterator, so nothing was posted and the prospect was lost | False branch posts with `resume_link: "None"`; the prospect is always recorded |
| Mail with two attachments | One POST per attachment ⇒ duplicate prospect records | Collapsed to one POST per email, first resume wins |
| `mag_name` | Parsed by the model from the subject | Parsed deterministically from the subject, model output as fallback |
| `experience_years` | Model only | Model first, subject (`17 Years & 0 Months`) as fallback |
| `candidate_name` | Model only | Model first, Gmail From display name as fallback (Naukri sets it to the candidate) |
| LinkedIn search failure | Run failed or notified | Continues; verdict becomes `NO` / confidence 0 and the prospect still reaches the portal |
| Google search | Relay's built-in scraper | SerpAPI via HTTP Request |
| Multi-email polls | One run per email | One execution can carry many emails; all Code nodes pair items explicitly |
| History actor | `Relay Webhook` | `n8n Webhook` |

---

## Server-Side Changes

| File | Change |
|------|--------|
| `Risebird.Web/Helpers/AutomationWebhookHelper.cs` | New. Shared secret validation (3 headers), UTC timestamp parsing and platform resolution for every automation webhook |
| `Risebird.Entities/ViewModel/NaukriProspectiveInterviewerVM.cs` | `RelayNaukriPayload.Source` added |
| `Risebird.Web/Controllers/NaukriOnboardingController.cs` | `Webhook` uses the shared helper; history actor and agent labels follow the reported platform |
| `Risebird.Web/Controllers/InterviewerAvailabilityController.cs` | Refactored onto the shared helper (its three private copies removed) |

`LinkedInWebhook` still uses its own inline `X-Relay-Secret` check — that workflow has not been
migrated yet and should move onto the shared helper when it is.

No database changes: `usp_InsertNaukriProspectiveInterviewer` already accepts everything sent.

---

## Fix — 14 Sep 2026: fewer tickets than Naukri responses

**Symptom.** For MAG "HIL Tester", 16 responses arrived on Naukri but only 11 tickets
appeared on `/NaukriOnboarding`. Similar gaps across other MAGs.

**Candidates are dropped at five points, and four of them leave no trace at all.**

| # | Drop point | Intended? |
|---|-----------|-----------|
| 1 | `Experience >= 8 years` — the false branch was deliberately unconnected | Was intended; **now files a Not Qualified ticket instead** |
| 2 | The same gate firing on an experience figure that could not be *parsed* | **No — bug, now fixed** |
| 3 | Gmail trigger `readStatus: unread` — a mail opened in the shared inbox before the poll is never processed | Inherited from Relay |
| 4 | `Only Naukri Interviewer Emails` — needs `@naukri.com` **and** subject containing "Online Interviewer" | Inherited from Relay |
| 5 | `Upload Resume to Drive` had no `onError`, so one Drive failure killed the whole poll batch | **No — bug, now fixed** |

Point 1 was the expected explanation for most of any given gap, and it was invisible by
design: a dropped candidate was written nowhere, so the page could not be reconciled against
Naukri by counting. **That is now changed** — see below.

### Under-experienced candidates now become Not Qualified tickets

The gate (renamed `Qualifies for onboarding`) routes both ways:

```
Qualifies for onboarding [TRUE ] -> Prepare LinkedIn Query -> … -> Finalize Record
Qualifies for onboarding [FALSE] ------------------------------> Finalize Record
```

The false branch deliberately skips the LinkedIn search, the AI verification and the Drive
upload — none of that is worth spending on a rejected candidate. It still reaches
`Send to Risebird Webhook`, carrying `qualifies_for_onboarding: false`. The server then calls
`SetFunnelOutcomeAsync(id, "NotQualified", "Closed")` and writes a `NotQualified` history
entry, exactly like a failed Employment Verification.

Every Naukri response that clears the Gmail filter now produces a row, so the page count
reconciles against Naukri and the difference is visible as the Not Qualified funnel stage.

`usp_GetEligibleProspectsForAutomation` excludes these twice over — `Status IN ('Open',
'InProgress')` and `FunnelStage NOT IN (…, 'NotQualified')` — so they consume no Employment
Verification, invitation emails or Plivo calls.

`qualifies_for_onboarding` is nullable server-side: legacy Relay payloads omit it, and absent
must mean qualified.

**Point 2 was the real defect.** `Merge Candidate` derived experience as:

```js
const years = modelYears > 0 ? modelYears : yearsFromSubject(subject);
```

so `0` meant *both* "fresher" and "could not read it". A candidate with 15 years whose
figure did not parse failed `>= 8` and vanished exactly like a junior. That matters more
than it looks, because the extractor currently contributes nothing (see below) — leaving
`yearsFromSubject` carrying the entire gate on its own.

The gate now tests an explicit `qualifies_for_onboarding`, which `Merge Candidate` sets to
true whenever the experience is *unknown*. Genuinely under-experienced candidates are still
dropped; unreadable ones reach the page for a human to judge. `experience_years` is reported
as blank rather than `"0"` when unknown, and `experience_known` records the difference.

---

## Resume never reached blob storage (fixed)

A 15 Sep run returned `resumeUploaded: false` despite a valid `resume_link` and
`has_attachment: true`. The link was:

```
https://docs.google.com/document/d/13PZBIA…/edit?usp=drivesdk&…
```

Drive converts an uploaded `.doc`/`.docx` resume into a **native Google Doc** and returns an
`/edit` link. `ConvertToDirectDownloadUrl` only rewrote `drive.google.com/file/d/{ID}` and
`open?id={ID}`, so this URL passed through untouched, the fetch returned the editor's HTML
page, the `isHtml` guard rejected it, and `blobResumeUrl` stayed null.

That matters beyond the missing blob copy: Naukri hides the candidate's email and phone
behind "View Contact Details", so both come from the resume. No resume bytes means
`candidate_email` and `candidate_phone` stay empty — and the outreach automation has nobody
to email or call.

`ConvertToDirectDownloadUrl` now rewrites Google Docs links to
`/export?format=docx`. `Share Resume File` already grants `role: reader, type: anyone`, so
the export needs no authentication. Both download call sites already map the
`wordprocessingml` content type to `.docx`, so the blob is stored with the right extension.
A resume that still cannot be fetched now writes a `ResumeFetchFailed` history entry rather
than leaving a prospect with no contact details and no explanation.

---

## Known broken — LinkedIn verification returns nothing

Every row shows `NO MATCH 0%`. The 15 Sep payload shows why:

```json
"search_result_count": 0,
"search_results_text": "NO SEARCH RESULTS AVAILABLE.",
"linkedin_match": "NO",
"linkedin_reason": "No Google search results were available, so there was no LinkedIn profile to compare against."
```

`Google Search (LinkedIn)` calls SerpAPI with `nodeCredentialType: serpApi` and returns zero
results for every query, so `Verify LinkedIn Match` is correctly answering NO against an
empty result set. The model and the workflow logic are both behaving; the search itself is
returning nothing, which points at the **SerpAPI credential** — missing, expired, or out of
quota. `onError: continueRegularOutput` on that node is what makes the failure silent.

This cannot be fixed in the workflow JSON. Check the SerpAPI credential and its quota.

---

## Resolved — extracted fields arriving empty

`CURRENT ROLE`, `LOCATION` and `KEY SKILLS` render blank (not the `—` placeholder the view
emits for null), so they are arriving as empty strings. The split is exactly along one line:

| Field | Source in `Merge Candidate` | On the page |
|-------|-----------------------------|-------------|
| `candidate_name` | model → **From display name** | populated |
| `experience_years` | model → **subject regex** | populated |
| `mag_name` | **subject regex** → model | partly populated |
| `location`, `current_role`, `key_skills`, `education`, `salary` | model only | **blank** |

Every field with a deterministic fallback is populated; every model-only field is empty.
That points at `Extract Candidate Data` returning nothing — the same class of failure the
Interviewer Availability workflow had, and consistent with `linkedin_match` reading
"NO MATCH 0%" on every row, since `Verify LinkedIn Match` shares the `GPT-5.4 mini` sub-node.

Both extractor nodes run `onError: continueRegularOutput`, so a failure is silent: the input
item passes through, `items[i].json.output` is `undefined`, and every extracted field becomes
`''`.

**Outcome:** a 15 Sep execution showed every one of those fields populated —
`location: "Bengaluru"`, `current_role: "Manager at Deloitte Consulting"`, full `key_skills`,
`education`, `salary`. So `Extract Candidate Data` is working, and the inference above did
not hold. Records created before that (the 14 Sep rows) still carry the empty strings and
would need reprocessing or manual correction; new records are fine.

---

## Setup Checklist

1. Import `Risebird-n8n-Naukri-Onboarding.json`. It arrives **inactive** on purpose.
2. Re-select the four credentials (Gmail `neha_dubey@risebird.io`, Google Drive, SerpAPI, OpenAI/n8n credits) — credential IDs do not always survive an import.
3. Confirm `gpt-5.4-mini` in the **GPT-5.4 mini** dropdown; Relay's `relay.gpt-5.4-mini` was an internal namespace and is not a valid model id.
4. Confirm the Drive folder resolves to `RB Naukri Prospective Interviewers` (`1WVeldaLKeabrPS6gCyh7kvyU1M4Vxetc`).
5. Confirm the Google Drive credential's OAuth scope allows managing permissions, otherwise `Share Resume File` fails (it continues on error, so the run still completes).
6. Verify `X-Webhook-Secret` on **Send to Risebird Webhook** matches `RelayWebhook:NaukriOnboardingSecret`.
7. Turn the Relay agent **off** before activating this workflow — both watch the same inbox and would each create a prospect for every email.
8. Activate, then check the first execution: `mag_name` should be the job title only, `experience_years` a bare whole number, and the prospect should appear under `/NaukriOnboarding` with an `n8n Webhook` history entry.
