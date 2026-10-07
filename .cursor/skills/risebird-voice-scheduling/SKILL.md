---
name: risebird-voice-scheduling
description: >-
  Plivo CX, Plivo WhatsApp, and Vapi outbound interview scheduling — eligibility
  SPs, timer jobs, webhooks, and schema joins. Use for voice agent or call
  scheduling work.
---

# Voice Scheduling

## Eligibility SPs
| Flow | SP |
|------|-----|
| Plivo CX schedule | `usp_GetEligibleCandidatesForPlivoCXCalls` |
| Plivo CX reschedule | `usp_GetEligibleCandidatesForPlivoCXReschedule` |
| Plivo WhatsApp schedule/reschedule | `usp_GetEligibleCandidatesForPlivoWhatsApp*` |
| Vapi (legacy) | `usp_GetEligibleCandidatesForVapiCalls` |

Schedule and reschedule SPs are mutually exclusive.

## Repository entry points
- `PlivoCXCallRepository.GetEligibleCandidatesForCallsAsync`
- Passes `@NowIST = DateTime.Now`

## Join keys
```
PlivoCXCallLogs: RecipientEmail + IRFId (int, = Candidate.IRFId)
InterviewMeetings: candidateEmail + IRFId (varchar, = irf.IRFId)
```

## Timer jobs
`PlivoCXInterviewSchedulingJob`, `PlivoCXRescheduleJob`, `PlivoWhatsAppScheduleJob`

## Optimize
Migration: `Risebird.DB/Migrations/Optimize_GetEligibleCandidatesForPlivoCXCalls.sql`

Details: `Docs/VoiceAgents.md`
