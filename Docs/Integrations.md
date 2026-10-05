# External Integrations

## SendGrid unsubscribe

Configure SendGrid subscription tracking + portal webhook/list management for candidate/interviewer comms compliance. Keys in appsettings under SendGrid section.

## n8n agents (live)

| Workflow | Doc |
|----------|-----|
| Interviewer email availability | [n8nAgent-InterviewerAvailability.md](n8nAgent-InterviewerAvailability.md) |
| Naukri onboarding | [n8nAgent-NaukriOnboarding.md](n8nAgent-NaukriOnboarding.md) |

Both POST structured payloads to Risebird webhook endpoints after GPT extraction.

## Relay.app (legacy)

[RelayAgent-InterviewerAvailability.md](RelayAgent-InterviewerAvailability.md) — replaced by n8n; kept for reference only.

## Other integrations (code)

| Service | Location |
|---------|----------|
| Zoom | `ZoomHelper`, timer `ZoomService` |
| Zoho Meeting | `ZohoMeetingHelper` |
| AWS Rekognition | `AWSRekognitionService` |
| Cashfree payouts | `CashfreePayoutService` |
| ElevenLabs | `ElevenLabsAgentService` |
| Affinda resume | Candidate resume parsing flows |

Register new integrations in `CommonHelper` + interface + DI in relevant `Program.cs`.
