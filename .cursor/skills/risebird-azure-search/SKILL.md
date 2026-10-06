---
name: risebird-azure-search
description: >-
  Azure AI Search listing indexes for Risebird — deploy scripts, batch SPs, full
  crawl endpoints, and appsettings. Use for IRF, call catalogue, interviewer,
  template, or raised issue search work.
---

# Azure Search Listings

## Deploy index
```powershell
cd Risebird.DB\Scripts
.\Create-CallCatalogueIndex.ps1 -SearchEndpoint "https://<svc>.search.windows.net" -AdminApiKey "<key>"
```

Scripts: `Create-IrfIndex.ps1`, `Create-InterviewerIndex.ps1`, `Create-RaisedIssueIndex.ps1`, `Create-TemplateIndex.ps1`

## After index create
1. Deploy `usp_Get*IndexBatch` SP
2. Set `AzureSearch:*Listing:Enabled=true` in appsettings
3. POST FullCrawl on Function App (e.g. `/api/CallCatalogueIndexSearch/FullCrawl?code=<key>`)

## Code
- Definitions: `Risebird.Repository/Indexing/*IndexDefinition.cs`
- Repos: `*ListingSearchRepository.cs`
- DI: `AzureSearchServiceCollectionExtensions.cs`

## Call catalogue
Index: `callcatalogueindex` | SP: `usp_GetCallCatalogueIndexBatch` | UI: `/HR/CallCatalogue`

Details: `Docs/AzureSearch.md`
