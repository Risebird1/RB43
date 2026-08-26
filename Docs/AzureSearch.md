# Azure AI Search

## Indexes

| Index | Script | Batch SP | Full crawl endpoint |
|-------|--------|----------|---------------------|
| `irfindex` | `Scripts/Create-IrfIndex.ps1` | `usp_GetIrfIndexBatch` | `/api/IrfIndexSearch/FullCrawl` |
| `callcatalogueindex` | `Scripts/Create-CallCatalogueIndex.ps1` | `usp_GetCallCatalogueIndexBatch` | `/api/CallCatalogueIndexSearch/FullCrawl` |
| `interviewerindex` | `Scripts/Create-InterviewerIndex.ps1` | (interviewer batch SP) | `/api/InterviewerIndexSearch/FullCrawl` |
| `raisedissueindex` | `Scripts/Create-RaisedIssueIndex.ps1` | `usp_GetRaisedIssueIndexBatch` | `/api/RaisedIssueIndexSearch/FullCrawl` |
| `templateindex` | `Scripts/Create-TemplateIndex.ps1` | `usp_GetTemplateIndexBatch` | `/api/TemplateIndexSearch/FullCrawl` |

Wrapper: `Scripts/Create-RaisedIssueAndTemplateIndexes.ps1` (both at once).

## Deploy index (example: Call Catalogue)

```powershell
cd Risebird.DB\Scripts

.\Create-CallCatalogueIndex.ps1 `
    -SearchEndpoint "https://<service>.search.windows.net" `
    -AdminApiKey "<admin-key>"
```

Optional full crawl after create:

```powershell
.\Create-CallCatalogueIndex.ps1 `
    -SearchEndpoint "https://<service>.search.windows.net" `
    -AdminApiKey "<admin-key>" `
    -FunctionAppUrl "https://<func-app>.azurewebsites.net" `
    -FunctionKey "<host-key>"
```

## App settings (per listing)

```json
"AzureSearch": {
  "Endpoint": "https://<service>.search.windows.net",
  "ApiKey": "<query-or-admin-key>",
  "CallCatalogueListing": {
    "Enabled": true,
    "IndexName": "callcatalogueindex",
    "PageSize": 50,
    "BatchSize": 500,
    "ManageIndexSchema": false
  }
}
```

Set `ManageIndexSchema: false` in production — schema managed by PS scripts.

## Code locations

- Field constants: `Risebird.Repository/Indexing/*IndexFields.cs`
- Index schema mirror: `*IndexDefinition.cs`
- Repository: `*ListingSearchRepository.cs`
- DI: `AzureSearchServiceCollectionExtensions.cs`

## Incremental sync

- Full crawl: timer/HTTP on Function App
- Single doc: upsert endpoints per index (e.g. `UpsertCandidate`, `UpsertInterviewer` for call catalogue)
- Portal actions may call `UserService.TrySyncCallCatalogue*` on candidate/interviewer updates
