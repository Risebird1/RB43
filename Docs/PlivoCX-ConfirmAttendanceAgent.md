# PlivoCX "Confirm attendance" agent — setup

A PlivoCX campaign can run an agent that only asks a candidate to confirm they will attend an
interview that is already booked. It collects no slot and books nothing. This page covers
building that flow in Plivo and connecting it to Risebird.

Until `PlivoCX:ConfirmTriggerEndpointUrl` is set, the **Confirm attendance** agent does not
appear on `/PlivoCXCampaign/Create`.

**Importable flow:** [PlivoCX-ConfirmAttendanceAgent-flow.json](PlivoCX-ConfirmAttendanceAgent-flow.json),
built on the existing *Outbound Interview Reschedule* flow export (same nodes, caller id, voice
and Hangup URL).

## What Risebird sends the flow

Every campaign call POSTs the same variables to the flow's API Request trigger
(`PlivoCXTriggerRequest`). For a confirm call:

| Variable | Value |
|---|---|
| `phone_number` | candidate's number, sanitised to +91 |
| `candidate_name`, `candidate_email` | the candidate |
| `job_title`, `client_name`, `irf_id` | the interview's skill channel |
| `call_purpose` | `confirm` |
| `original_interview_date` | the meeting to confirm, e.g. `09 Oct 2026, 3:00 PM` |
| `today_date`, `tomorrow_date`, `day_after_tomorrow_date` | e.g. `Thursday, 15 October 2026` |
| `log_id` | Risebird's call-log id — **must be sent back in the hangup event** |

## What the flow must send back

Configure **Agent Config → Events → Hangup URL** as
`https://portal.risebird.io/PlivoCXInterviewCall/HangupCallback` (the same URL as the schedule
and reschedule flows), with recording turned on as in those flows.

Risebird reads these flow variables from the hangup event. A node prefix such as
`Conversation.candidate_confirmed` is fine:

| Variable | Values | Used for |
|---|---|---|
| `log_id` | echo of the trigger value | matching the call to its campaign candidate |
| `candidate_confirmed` | `true` / `false` | `true` → candidate **Booked** (shown as "Confirmed") |
| `candidate_disposition` | `interested` / `uninterested` / `cant_say` | outcome column; `interested` also counts as a yes |
| `summary` | short text | Call detail → Analysis |

Do **not** set `interview_date` / `interview_time`. The webhook skips booking for this agent anyway,
because its `PlivoCXAgent.CollectsSlot` is 0.

## Suggested AI Conversation node instructions

> You are calling {{candidate_name}} on behalf of Risebird about their interview for
> {{job_title}} with {{client_name}}, scheduled for {{original_interview_date}}. Today is
> {{today_date}}.
>
> 1. Confirm you are speaking to {{candidate_name}}. If not, apologise and end the call.
> 2. Remind them of the interview date and time and ask whether they will attend.
> 3. If yes, thank them, remind them to join on time from a quiet place with a working camera
>    and microphone, and end the call. Set `candidate_confirmed` = true and
>    `candidate_disposition` = interested.
> 4. If they cannot make it or want another time, do not offer or book a new slot. Tell them the
>    Risebird team will contact them to reschedule. Set `candidate_confirmed` = false and
>    `candidate_disposition` = cant_say.
> 5. If they no longer want the interview, thank them and end the call. Set
>    `candidate_confirmed` = false and `candidate_disposition` = uninterested.
> 6. Keep the call under two minutes. Write a one or two sentence `summary` of the outcome.

## Turning it on

1. In cx.plivo.com, open the flow → API Request trigger node → copy the **API URL**.
2. Set it as `PlivoCX:ConfirmTriggerEndpointUrl` in **both** `Risebird.Web` and
   `Risebird.AzureTimerFunctions` config. In Azure app settings the key is
   `PlivoCX__ConfirmTriggerEndpointUrl`.
3. Open `/PlivoCXCampaign/Create`. The agent row is created on first load.

## What a "yes" does in Risebird

- The campaign candidate becomes **Booked**, with the booked-slot column reading
  `Confirmed · <meeting date>`.
- The meeting is marked candidate-confirmed (`InterviewMeetings.IsCandidateResponsed = 1,
  CandidateResponse = 1`), exactly as the candidate's confirmation email link does. It then
  leaves `/Dashboard/UnconfirmedCandidates`. This needs the campaign candidate's `InterviewId`,
  which the Unconfirmed list and the By IRF picker both supply.
- A "no" never cancels the meeting. The candidate shows as **Not booked** for Ops to follow up,
  for example with a Reschedule campaign.

## Testing

1. Create a campaign with agent **Confirm attendance** and source **Unconfirmed**. Pick one
   candidate whose number is yours.
2. Use **Test call** first to hear the flow, then **Call** on the row.
3. Answer "yes". Within a few seconds of hangup the row should read Booked / Confirmed. Check
   the meeting:

   ```sql
   SELECT id, IsCandidateResponsed, CandidateResponse, confirmationStatus
   FROM InterviewMeetings WHERE id = <InterviewId>;
   ```
