# OpenAI Token Usage Not Being Captured - ROOT CAUSE & FIX

## 🚨 Critical Discovery

**User Report**: `EstimatedCostUSD` column in `OpenAIApiLogs` table shows 0 for all entries except Whisper-1 calls.

**Root Cause Found**: The OpenAI **Assistants API v2 responses DO NOT include token usage information**!

---

## Why Token Usage Was Always Zero

### The Problem

The Assistants API has a fundamentally different response structure than the Chat Completions API:

#### ❌ What We Were Doing (WRONG):
```
1. Call CreateRun → Get response (NO usage data)
2. Call RetrieveRun (poll) → Get response (NO usage data)  
3. Call RetrieveRun (poll) → Get response (NO usage data)
4. Call RetrieveRun (final) → Get response with status="completed" (STILL NO usage data!)
5. Log each call → Extract tokens → Find NONE → Save cost as $0.00
```

#### ✅ What We SHOULD Be Doing (CORRECT):
```
1. Complete the run
2. Call "List Run Steps" API → Get response (HAS usage data!)
3. Extract tokens from run steps
4. Calculate and save cost
```

### Why Whisper-1 Had Costs

Whisper (speech-to-text) uses a different API endpoint (`/audio/transcriptions`) that:
- Doesn't use the Assistants API
- Has a different pricing model (per minute of audio, not per token)
- Uses `EstimateWhisperCost()` method instead of token-based calculation

So whisper costs were being saved correctly because we had special handling for it!

---

## The Technical Details

### Assistants API Response Structure

**CreateRun Response** (v2):
```json
{
  "id": "run_abc123",
  "object": "thread.run",
  "created_at": 1699063290,
  "assistant_id": "asst_abc123",
  "thread_id": "thread_abc123",
  "status": "queued",
  "model": "gpt-4o",
  // ❌ NO "usage" field here!
}
```

**RetrieveRun Response** (status: completed):
```json
{
  "id": "run_abc123",
  "object": "thread.run",
  "status": "completed",
  "model": "gpt-4o",
  "assistant_id": "asst_abc123",
  "thread_id": "thread_abc123",
  // ❌ STILL NO "usage" field here!
  "completed_at": 1699063291,
  "started_at": 1699063290
}
```

**List Run Steps Response** (WHERE THE USAGE DATA IS!):
```json
{
  "object": "list",
  "data": [
    {
      "id": "step_abc123",
      "object": "thread.run.step",
      "type": "message_creation",
      "status": "completed",
      "step_details": {
        "type": "message_creation",
        "message_creation": {
          "message_id": "msg_abc123"
        }
      },
      "usage": {                          // ✅ HERE IT IS!
        "prompt_tokens": 123,
        "completion_tokens": 456,
        "total_tokens": 579
      }
    }
  ]
}
```

---

## The Fix Applied

### 1. Enhanced Token Extraction

Updated `OpenAIApiLogger.ExtractTokenUsage()` method to handle:
- Standard `usage` object (Chat Completions API)
- CamelCase variants (some SDKs return this)
- Run steps format (nested in data array)
- Deep search for usage object anywhere in response

**File**: `Risebird.CommonHelper\OpenAIApiLogger.cs`

```csharp
// Added support for run steps format
if (objectType == "list" && json["data"] != null)
{
    var dataArray = json["data"] as JArray;
    if (dataArray != null && dataArray.Count > 0)
    {
        int totalPromptTokens = 0;
        int totalCompletionTokens = 0;
        
        // Sum up usage from all steps
        foreach (var step in dataArray)
        {
            var stepUsage = step["usage"];
            if (stepUsage != null)
            {
                totalPromptTokens += stepUsage["prompt_tokens"]?.Value<int>() ?? 0;
                totalCompletionTokens += stepUsage["completion_tokens"]?.Value<int>() ?? 0;
            }
        }
        
        return (totalPromptTokens, totalCompletionTokens, totalPromptTokens + totalCompletionTokens);
    }
}
```

### 2. Added Run Steps Retrieval

Created new method `RetrieveRunStepsForTokenUsage()` in both:
- `AssistantServiceHelper.cs` 
- `OpenAIInterviewerAssistantHelper.cs`

This method:
1. Calls the `/threads/{threadId}/runs/{runId}/steps` endpoint
2. Gets the response with usage data
3. Logs it (which triggers token extraction and cost calculation)

### 3. Call After Run Completes

Updated all run polling loops to retrieve run steps after completion:

```csharp
while (runStatus != "completed")
{
    await Task.Delay(pollInterval);
    runStatus = await RetrieveRun(runId, threadId);
    if (runStatus == "failed")
        break;
    pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
}

// ✅ NEW: Retrieve run steps to capture token usage
if (runStatus == "completed")
{
    await RetrieveRunStepsForTokenUsage(runId, threadId);
}
```

---

## What This Means

### Before Fix:
- ❌ Every Assistants API call logged with `PromptTokens = 0`, `CompletionTokens = 0`, `EstimatedCostUSD = 0`
- ❌ No way to track actual OpenAI spending for Assistants
- ❌ No audit trail of token usage
- ✅ Only Whisper calls had costs (because they use different calculation)

### After Fix:
- ✅ Assistants API calls still logged (for audit purposes)
- ✅ Additional "RunStepsRetrieval-TokenUsage" log entry created with actual usage
- ✅ Token counts captured: `PromptTokens`, `CompletionTokens`, `TotalTokens`
- ✅ Cost calculated correctly based on model pricing
- ✅ Complete audit trail of spending

---

## Important Notes

### Additional API Call

The fix adds **one additional API call per completed run**:
- Endpoint: `GET /v1/threads/{threadId}/runs/{runId}/steps`
- When: After each run completes successfully
- Cost: Included in OpenAI API pricing (no extra charge for the call itself)
- Token usage: The call itself doesn't consume tokens, it just retrieves the usage data

### Log Entries

For each Assistants API run, you'll now see **multiple log entries**:

1. `CreateRun` - Creates the run (no tokens)
2. `RunRetrieval` (multiple) - Status checks while running (no tokens)
3. `RunRetrieval` (final) - Status is "completed" (no tokens)
4. **`RunStepsRetrieval-TokenUsage`** - ✅ **HAS TOKENS AND COST!**
5. `MessageRetrieval` - Gets the messages (no additional tokens)

This is normal and expected! The run steps entry is where the actual token usage and cost are recorded.

### Querying Costs

To get actual costs, query for entries where tokens > 0:

```sql
-- Get total cost (only entries with tokens)
SELECT 
    SUM(EstimatedCostUSD) as TotalCost,
    SUM(PromptTokens) as TotalPromptTokens,
    SUM(CompletionTokens) as TotalCompletionTokens,
    SUM(TotalTokens) as TotalTokensUsed,
    COUNT(*) as CallsWithTokenUsage
FROM OpenAIApiLogs
WHERE TotalTokens > 0
    AND LogDate >= DATEADD(day, -30, GETDATE());

-- Get cost by operation type (only entries with tokens)
SELECT 
    OperationType,
    COUNT(*) as Calls,
    SUM(TotalTokens) as TotalTokens,
    SUM(EstimatedCostUSD) as TotalCost,
    AVG(EstimatedCostUSD) as AvgCostPerCall
FROM OpenAIApiLogs
WHERE TotalTokens > 0
    AND LogDate >= DATEADD(day, -30, GETDATE())
GROUP BY OperationType
ORDER BY TotalCost DESC;
```

---

## Verification Steps

### After Deployment:

1. **Trigger an Assistants API call** (e.g., resume screening, candidate screening)

2. **Check the logs**:
```sql
-- Find recent run steps entries
SELECT TOP 10
    Id,
    LogDate,
    Model,
    OperationType,
    PromptTokens,
    CompletionTokens,
    TotalTokens,
    EstimatedCostUSD,
    RunId
FROM OpenAIApiLogs
WHERE OperationType LIKE '%RunSteps%'
ORDER BY LogDate DESC;
```

**Expected**: You should see entries with:
- `OperationType = "RunStepsRetrieval-TokenUsage"` or `"AIInterview-RunStepsRetrieval-TokenUsage"`
- `PromptTokens > 0`
- `CompletionTokens > 0`  
- `EstimatedCostUSD > 0`
- `Model = "gpt-4o"` or similar

3. **Verify cost calculation**:
```sql
SELECT TOP 10
    LogDate,
    Model,
    PromptTokens,
    CompletionTokens,
    EstimatedCostUSD,
    -- Calculate expected cost for gpt-4o
    (PromptTokens / 1000.0 * 0.0025 + CompletionTokens / 1000.0 * 0.0100) as ExpectedCost,
    -- Check if they match
    ABS(EstimatedCostUSD - (PromptTokens / 1000.0 * 0.0025 + CompletionTokens / 1000.0 * 0.0100)) as Difference
FROM OpenAIApiLogs
WHERE OperationType LIKE '%RunSteps%'
    AND TotalTokens > 0
ORDER BY LogDate DESC;
```

**Expected**: `Difference` should be very small (< 0.000001) due to rounding.

---

## Files Modified

1. ✅ **`Risebird.CommonHelper\OpenAIApiLogger.cs`**
   - Enhanced `ExtractTokenUsage()` method to handle run steps format
   - Added support for nested usage objects
   - Added deep search for usage data

2. ✅ **`Risebird.CommonHelper\AssistantServiceHelper.cs`**
   - Added `RetrieveRunStepsForTokenUsage()` method
   - Updated all run polling loops to call it after completion (7 locations)

3. ✅ **`Risebird.CommonHelper\OpenAIInterviewerAssistantHelper.cs`**
   - Added `RetrieveRunStepsForTokenUsage()` method
   - Updated `WaitForRunAsync()` to call it after completion

---

## Historical Data

### Can We Fix Old Records?

**Unfortunately, NO.** The token usage data is only available at the time the run completes. We can't retroactively retrieve it because:
- Run steps are only kept for a limited time by OpenAI
- Older runs may have been deleted
- No way to recalculate usage after the fact

### What About Old Logs?

Old log entries where `EstimatedCostUSD = 0` are **not recoverable**. However:
- They still provide audit trail (which calls were made, when, to which endpoints)
- Starting from deployment, ALL new runs will have correct costs
- You can establish a baseline from deployment date forward

---

## Comparison with Original Issues

### Issue #1: Wrong Pricing ✅ FIXED
- Updated pricing constants to correct gpt-4o pricing ($0.0025/$0.0100)
- Fixed model detection order

### Issue #2: Model Name Not Captured ✅ FIXED
- Extracting model from responses before logging
- Model names now saved correctly

### Issue #3: Token Usage Not Being Saved ✅ FIXED (THIS DOCUMENT)
- Root cause: Assistants API doesn't return usage in standard responses
- Solution: Retrieve run steps after completion
- Token usage now captured and saved correctly

---

## Summary

| Aspect | Before Fix | After Fix |
|--------|-----------|-----------|
| Token counts | Always 0 | Correct values from run steps |
| Cost | Always $0.00 | Calculated correctly |
| Model name | NULL or incorrect | Correct model name |
| Audit trail | Incomplete | Complete with usage data |
| API calls per run | N (create + polls + messages) | N + 1 (+ run steps) |

---

**Date**: January 31, 2026  
**Status**: Fix Complete - Ready for Deployment  
**Impact**: CRITICAL - Enables accurate cost tracking for Assistants API usage
