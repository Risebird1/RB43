# OpenAI Cost & Logging

## Tables

| Table | Purpose |
|-------|---------|
| `OpenAIApiLogs` | Per-request token/cost log |
| `OpenAIAssistantCostSummary` | Aggregated cost by `AssistantId` |

## Stored procedures

| SP | Purpose |
|----|---------|
| `usp_RefreshAssistantCostSummary` | Rebuild summary from logs (run periodically) |
| `usp_GetAssistantCostRanking` | Top assistants by cost |
| `usp_GetAssistantCostDetails` | Drill-down for one assistant |

## Repository

`IOpenAIApiLogRepository` — refresh + ranking queries consumed by admin/reporting UI.

## Cost optimization practices

1. Use smallest model that meets quality bar for each assistant operation
2. Refresh assistant cost summary weekly; investigate top spenders
3. Cache repeated prompt context where safe
4. Purge old logs via `usp_PurgeOldData` (`Purge:OpenAIRetentionDays`)

## Logging fixes (historical)

Token usage and cost fields must be populated on every OpenAI API response handler — check `OpenAIResponsesHelper`, `AssistantServiceHelper` when adding new call sites.

## Assistant cost tracking workflow

```
API call → log row in OpenAIApiLogs (tokens, model, assistantId, cost)
         → periodic usp_RefreshAssistantCostSummary
         → dashboard / ranking SPs
```

When adding a new OpenAI integration: always log through the shared helper path so cost attribution works.
