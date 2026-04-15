# Relay.app Agent — Interviewer Email Availability

## Overview

This Relay agent monitors a Gmail inbox for unread emails from interviewers sharing their availability, extracts structured data using GPT-5.4 Mini, optionally uploads resumes to Google Drive, writes a row to a Google Sheet, and POSTs the extracted data to a webhook on our server.

---

## Flow Architecture

```
🔁 Gmail Batch Trigger (unread emails, one run per email)
  │
  ├─ 🚫 Skip if older than 46 hours
  ├─ 🚫 Skip system emails (mailer-daemon, workspace-noreply)
  │
  ├─ 🤖 GPT-5.4 Mini — Extract 13 fields from email + first attachment
  │
  ├─ 🔀 Branch: Has attachment?
  │     ├─ YES → Upload resume to Google Drive → Write to Sheet (with resume link)
  │     └─ NO  → Write to Sheet (resume link = "None")
  │
  └─ 📤 HTTP POST → Webhook endpoint on Risebird server
```

---

## Step-by-Step Configuration

### 1. Trigger — Batch Trigger on Gmail

- **Type:** Batch trigger
- **Source:** Gmail
- **Filter:** Only unread emails
- **Behavior:** Creates a **separate run** for each matching email

### 2. Skip if Older Than 46 Hours

- **Type:** Condition / End Run
- **Logic:** If `email.send_date` is more than 46 hours before `now()`, end the run
- **Purpose:** Prevents processing stale emails that may have already been handled manually

### 3. Skip System Emails

- **Type:** Condition / End Run
- **Logic:** If sender is one of:
  - `mailer-daemon@googlemail.com`
  - `workspace-noreply@google.com`
- **Action:** End the run immediately

### 4. Extract Interviewer Data (GPT-5.4 Mini)

- **Type:** AI Step (Generate Text / Structured Output)
- **Model:** GPT-5.4 Mini
- **Attachments:** Full email body + first attachment (resume, if present)
- **System Prompt:**

```
You are an AI assistant that extracts structured interviewer availability data from emails.

Given the email subject, body, sender information, and optionally a resume attachment, extract the following 13 fields. Return ONLY valid JSON with these exact keys:

{
  "job_title": "The job title or role mentioned in the subject or body. If not found, use 'N/A'.",
  "name": "The sender's full name, extracted from the From field or email signature.",
  "email": "The sender's email address.",
  "phone": "Phone number found in the body, signature, or resume. If not found, use 'N/A'.",
  "mag_name": "The magazine name, project name, or publication referenced in the email. If not found, use 'N/A'.",
  "irf_id": "Any IRF ID, reference number, or skill channel ID mentioned. If not found, use 'N/A'.",
  "has_login_credentials": "'Yes' if the email mentions login credentials, portal access, or account details. Otherwise 'No'.",
  "slots_per_day": "The number of interview slots the interviewer can take per day (e.g. '2', '3'). If not specified, use 'N/A'.",
  "time_frame": "One or more of these time windows: '6-10 AM', '10 AM-2 PM', '2-6 PM', '6-10 PM', '10 PM-12 AM'. Comma-separated if multiple. If not specified, use 'N/A'.",
  "availability_intent": "A summary of the interviewer's stated availability — dates, times, days of week, or general availability windows. Be specific.",
  "mail_body_intent": "A one-line summary of the email's purpose (e.g. 'Sharing availability for next week', 'Requesting slot change', 'Confirming availability').",
  "concerns_or_not_interested": "Any hesitations, concerns, conditions, or expressions of disinterest. If none, use 'None'.",
  "resume_fitment": "If a resume is attached, assess fitment: 'Strong Fit', 'Moderate Fit', 'Weak Fit'. If no resume, use 'N/A'."
}

Rules:
- Extract phone numbers in their original format (include country code if present)
- For time_frame, only use the exact window labels listed above
- For resume_fitment, base your assessment on the resume's relevance to the job title mentioned
- Always return valid JSON, nothing else
```

- **User Prompt:** `Email Subject: {{email.subject}}\n\nFrom: {{email.from}}\n\nEmail Body:\n{{email.body}}`

### 5. Branch: Has Attachment?

- **Type:** Condition / Branch
- **Condition:** `email.attachments.length > 0`

#### Path A — Has Resume Attachment

**5a. Upload to Google Drive**
- **Folder:** `RB Interviewer Email Availability Internal`
- **Filename:** `{{extracted.name}} - {{extracted.job_title}} - Resume`
- **File:** First attachment from the email

**5b. Write to Google Sheet (with resume)**
- **Sheet:** `RB Interviewer Email Availability Relay Internal · Sheet1`
- **Columns:** All 13 extracted fields + `Resume Link` = Drive file URL

#### Path B — No Attachment

**5c. Write to Google Sheet (without resume)**
- **Sheet:** `RB Interviewer Email Availability Relay Internal · Sheet1`
- **Columns:** All 13 extracted fields + `Resume Link` = `"None"`

### 6. HTTP POST — Webhook to Risebird Server

- **Type:** HTTP Request
- **Method:** POST
- **URL:** `https://portal.risebird.io/InterviewerAvailability/Webhook`
- **Headers:**
  - `Content-Type: application/json`
  - `X-Relay-Secret: A72B9F41-E3D8-4A5C-B180-CF81E9D3A742`
- **Body (JSON):**

```json
{
  "sender_email": "{{extracted.email}}",
  "subject": "{{email.subject}}",
  "mail_content": "{{email.body}}",
  "received_at": "{{email.send_date}}",
  "job_title": "{{extracted.job_title}}",
  "name": "{{extracted.name}}",
  "phone": "{{extracted.phone}}",
  "mag_name": "{{extracted.mag_name}}",
  "irf_id": "{{extracted.irf_id}}",
  "has_login_credentials": "{{extracted.has_login_credentials}}",
  "slots_per_day": "{{extracted.slots_per_day}}",
  "time_frame": "{{extracted.time_frame}}",
  "availability_intent": "{{extracted.availability_intent}}",
  "mail_body_intent": "{{extracted.mail_body_intent}}",
  "concerns_or_not_interested": "{{extracted.concerns_or_not_interested}}",
  "resume_fitment": "{{extracted.resume_fitment}}",
  "resume_link": "{{drive_file_url OR 'None'}}",
  "has_attachment": {{email.attachments.length > 0}}
}
```

---

## Google Sheet Output

**Sheet name:** `RB Interviewer Email Availability Relay Internal · Sheet1`

| Column | Source |
|--------|--------|
| Job Title | `extracted.job_title` |
| Name | `extracted.name` |
| Email | `extracted.email` |
| Phone | `extracted.phone` |
| MagName | `extracted.mag_name` |
| IRF ID | `extracted.irf_id` |
| Has Login Credentials | `extracted.has_login_credentials` |
| Slots Per Day | `extracted.slots_per_day` |
| Time Frame | `extracted.time_frame` |
| Availability Intent | `extracted.availability_intent` |
| Mail Body Intent | `extracted.mail_body_intent` |
| Concerns or Not Interested | `extracted.concerns_or_not_interested` |
| Resume Fitment | `extracted.resume_fitment` |
| Resume Link | Drive URL or "None" |

---

## Webhook Response Handling

The server responds with:

```json
{
  "success": true,
  "id": 42,
  "message": "Interviewer availability record created (#42) for interviewer@example.com."
}
```

No further action is needed from Relay after the webhook fires. The server takes over the workflow (Plivo calls, verification, etc.).

---

## Security

- The webhook validates the `X-Relay-Secret` header against `RelayWebhook:InterviewerAvailabilitySecret` in appsettings.json
- Use a unique secret per workflow (different from the SupportTicket webhook secret)
