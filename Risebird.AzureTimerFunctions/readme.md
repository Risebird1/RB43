# Risebird Azure Timer Functions

Scheduled jobs: Plivo CX/WhatsApp calling, Vapi scheduling, Azure Search crawls, data purge, resume parsing, and more.

**Documentation:** [Docs/README.md](../Docs/README.md) — see [Deployment.md](../Docs/Deployment.md) and [VoiceAgents.md](../Docs/VoiceAgents.md).

## Local run

Configure `local.settings.json` with connection strings and API keys matching `appsettings.json`.

## Key functions

| Function | Purpose |
|----------|---------|
| `PlivoCXInterviewSchedulingJob` | Outbound interview schedule calls |
| `PlivoCXRescheduleJob` | Reschedule calls |
| `CallCatalogueIndexSearchFullCrawl` | Azure Search full crawl |
| `PurgeOldDataJob` | Retention purge |
