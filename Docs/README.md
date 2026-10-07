# Risebird Documentation

Token-efficient reference for developers and AI agents. Read **only the doc that matches your task**.

| Doc | Use when |
|-----|----------|
| [Architecture.md](Architecture.md) | Solution layout, layers, key projects |
| [VoiceAgents.md](VoiceAgents.md) | Vapi, Plivo CX voice, Plivo WhatsApp scheduling |
| [Database.md](Database.md) | SQL schema quirks, migrations, stored procedures |
| [AzureSearch.md](AzureSearch.md) | Listing indexes (IRF, Call Catalogue, etc.) |
| [Deployment.md](Deployment.md) | Deploy order, config keys, timer functions |
| [AIInterview.md](AIInterview.md) | AI interview webhooks, typing/listening assessments |
| [OpenAI.md](OpenAI.md) | OpenAI cost logging and assistant tracking |
| [Integrations.md](Integrations.md) | SendGrid, n8n agents, external webhooks |
| [n8nAgent-InterviewerAvailability.md](n8nAgent-InterviewerAvailability.md) | Live n8n interviewer availability workflow |
| [n8nAgent-NaukriOnboarding.md](n8nAgent-NaukriOnboarding.md) | Naukri onboarding n8n workflow |
| [RelayAgent-InterviewerAvailability.md](RelayAgent-InterviewerAvailability.md) | Legacy Relay.app (replaced by n8n) |

## Agent skills (Cursor)

Project skills live in `.cursor/skills/` — prefer reading a skill over loading full docs when the task is scoped.

## Database scripts

| Path | Purpose |
|------|---------|
| `Risebird.DB/Tables/` | Table DDL |
| `Risebird.DB/StoredProcedures/` | Canonical SP definitions |
| `Risebird.DB/Migrations/` | One-off deploy scripts |
| `Risebird.DB/Scripts/` | Azure Search index PowerShell scripts |
