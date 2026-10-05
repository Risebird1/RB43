# Risebird Architecture

## Solution layers

```
Risebird.Web              MVC portal (controllers, views)
Risebird.Services         API / webhook hosts
Risebird.AzureTimerFunctions   Scheduled jobs (Plivo, Vapi, index crawls, purge)
Risebird.MicroService     Background microservices
Risebird.Business         Business logic
Risebird.Business.Interface
Risebird.Repository       Dapper/SQL data access
Risebird.Repository.Interface
Risebird.CommonHelper     External integrations (Plivo, OpenAI, Azure, SendGrid)
Risebird.CommonHelper.Interface
Risebird.Entities         Models, VMs, DTOs
Risebird.DB               SQL scripts (Tables, StoredProcedures, Migrations, Scripts)
```

**Dependency flow:** Web/Services/Functions → Business → Repository → SQL Server.

## Key registration points

| Host | File | Registers |
|------|------|-----------|
| Web | `Risebird.Web/Program.cs` | Azure Search, Vapi, Plivo CX/WhatsApp, core services |
| Functions | `Risebird.AzureTimerFunctions/IServiceCollectionExtensions.cs` | Timer job DI |
| Services | `Risebird.Services/Program.cs` | API + Azure Search |
| MicroService | `Risebird.MicroService/Program.cs` | Azure Search |

## Azure Search listing plugins

Registered via `AddRisebirdAzureSearch()` in `Risebird.Repository/Search/AzureSearchServiceCollectionExtensions.cs`:

| Config key | Index | UI |
|------------|-------|-----|
| `AzureSearch:IrfListing` | `irfindex` | IRF listings |
| `AzureSearch:InterviewerListing` | interviewer index | Interviewer search |
| `AzureSearch:CallCatalogueListing` | `callcatalogueindex` | `/HR/CallCatalogue` |
| `AzureSearch:RaisedIssueListing` | `raisedissueindex` | Raised issues |
| `AzureSearch:TemplateListing` | `templateindex` | Templates |

Each listing: Repository in `Risebird.Repository/Indexing/`, batch SP `usp_Get*IndexBatch`, PS deploy script in `Risebird.DB/Scripts/`.

## Voice scheduling (high level)

```
Timer Function / Manual UI
    → Business (PlivoInterviewService / VapiInterviewService)
    → CommonHelper (PlivoCXService / VapiService) — outbound API
    → Repository — call logs, eligibility SPs
    → Webhook controllers — hangup callbacks, booking API
    → InterviewMeetings + Candidate disposition updates
```

See [VoiceAgents.md](VoiceAgents.md) for channel-specific detail.

## Config

Primary config: `Risebird.Web/appsettings.json`, mirrored in `Risebird.AzureTimerFunctions/appsettings.json` for timer jobs.

Secrets (connection strings, API keys) are environment-specific — never commit production values.
