
# OpenAI Assistant Cost Tracking Feature

## 🎯 Overview

This feature adds precise cost tracking at the **AssistantId level**, allowing you to:
- ✅ Identify which assistants are most expensive
- ✅ Track costs by assistant over time
- ✅ Compare performance across different assistants
- ✅ Optimize your most expensive operations

---

## 📊 What Was Added

### 1. New Database Table: `OpenAIAssistantCostSummary`

Aggregates cost data by AssistantId for fast querying:

**Key Fields:**
- `AssistantId` - Unique identifier for each assistant
- `TotalCostUSD` - Total cost incurred by this assistant
- `TotalCalls` - Number of API calls
- `CompletedRuns` - Number of completed runs
- `TotalTokens` - Total tokens consumed
- `Model` - Primary model used
- `PrimaryOperationType` - Most common operation type
- Performance metrics (success rate, avg response time, etc.)

### 2. New Stored Procedures

**`usp_RefreshAssistantCostSummary`**
- Recalculates all assistant cost summaries
- Run this periodically to update the data
- Takes 5-30 seconds depending on log volume

**`usp_GetAssistantCostRanking`**
- Returns assistants ranked by cost
- Supports filtering by date range, top N, min cost
- Fast queries using summary table

**`usp_GetAssistantCostDetails`**
- Detailed breakdown for a specific assistant
- Cost by operation type, model, and daily trend
- Multiple result sets for comprehensive analysis

### 3. New Repository Methods

**`IOpenAIApiLogRepository` Interface:**
```csharp
Task RefreshAssistantCostSummaryAsync();
Task<List<OpenAIAssistantCostSummary>> GetAssistantCostRankingAsync(
    int? topN = null, 
    decimal? minCost = null, 
    DateTime? startDate = null, 
    DateTime? endDate = null
);
Task<AssistantCostDetails> GetAssistantCostDetailsAsync(
    string assistantId, 
    DateTime? startDate = null, 
    DateTime? endDate = null
);
```

### 4. New Controller Actions

**`OpenAILogsController`:**
- `AssistantRanking` - Shows all assistants ranked by cost
- `AssistantDetails` - Detailed cost breakdown for one assistant
- `RefreshAssistantSummary` - Refresh the summary data

### 5. New Views

**`AssistantRanking.cshtml`**
- Table showing all assistants with costs
- Filtering by top N, min cost, date range
- Summary cards with totals
- Refresh button to update data
- 🏆 Medal badges for top 3 most expensive

**`AssistantDetails.cshtml`**
- Detailed cost breakdown for specific assistant
- Performance metrics (success rate, avg cost, etc.)
- Cost by operation type
- Cost by model
- Daily cost trend chart
- Token usage breakdown

---

## 🚀 Deployment Steps

### Step 1: Deploy Database Changes

Run the deployment script:
```sql
-- Execute this file in SQL Server Management Studio
Risebird.DB\Deploy_Assistant_Cost_Tracking.sql
```

This will:
- ✅ Create `OpenAIAssistantCostSummary` table
- ✅ Create indexes for performance
- ✅ Create stored procedures
- ✅ Load initial data from existing logs

**Expected output:**
```
✓ Table OpenAIAssistantCostSummary created successfully.
✓ Indexes created.
✓ Stored procedures created.
✓ Initial data loaded.
Total Assistants Tracked: X
Total Cost Tracked: $XXX.XX
```

### Step 2: Build & Deploy Code

Build these projects in order:
```
1. Risebird.Entities (new entity classes)
2. Risebird.Repository.Interface (new methods)
3. Risebird.Repository (implementation)
4. Risebird.Web (controllers & views)
```

**PowerShell:**
```powershell
cd "c:\Users\harsh\source\repos\Risebird"
dotnet build Risebird.Entities\Risebird.Entities.csproj --configuration Release
dotnet build Risebird.Repository.Interface\Risebird.Repository.Interface.csproj --configuration Release
dotnet build Risebird.Repository\Risebird.Repository.csproj --configuration Release
dotnet build Risebird.Web\Risebird.Web.csproj --configuration Release
```

### Step 3: Deploy to Servers

Deploy the updated DLLs to:
- ✅ Risebird.Web application pool
- ✅ Restart application pools

### Step 4: Verify Deployment

1. Navigate to: `/OpenAILogs/AssistantRanking`
2. You should see a list of assistants with costs
3. Click on any assistant to see detailed breakdown

---

## 📖 How to Use

### View Assistant Cost Ranking

**URL:** `/OpenAILogs/AssistantRanking`

**Features:**
- See all assistants ranked by cost (most expensive first)
- Filter by:
  - Top N (e.g., show only top 10)
  - Minimum cost (e.g., only show assistants costing > $5)
  - Date range (analyze specific time periods)
- Summary cards show totals across all assistants
- 🏆 Top 3 most expensive assistants get medal badges

**What you see:**
- Assistant ID
- Primary purpose (inferred from operations)
- Model used
- Total cost
- Number of runs
- Number of API calls
- Total tokens
- Average cost per run
- Success rate
- Active period (first to last used)

### View Detailed Breakdown

**URL:** `/OpenAILogs/AssistantDetails/{assistantId}`

**Features:**
- Complete cost analysis for one assistant
- Summary metrics:
  - Total cost
  - Completed runs
  - Total API calls
  - Total tokens
- Performance metrics:
  - Success rate
  - Average cost per run
  - Average response time
  - Days active
- Cost breakdowns:
  - By operation type (which operations cost the most?)
  - By model (which models are being used?)
  - Daily trend (cost over time chart)
  - Token usage (prompt vs completion tokens)

### Refresh Summary Data

Click the **"Refresh Summary Data"** button on the Assistant Ranking page.

**When to refresh:**
- Daily (recommended)
- After making code changes that affect costs
- When you notice the "Last Updated" timestamp is old
- After deploying the token usage fix

**What it does:**
- Recalculates all assistant costs from raw logs
- Updates the summary table
- Takes 5-30 seconds depending on data volume

---

## 💡 Use Cases

### 1. Identify Expensive Assistants

**Goal:** Find which assistants are costing the most money.

**Steps:**
1. Go to `/OpenAILogs/AssistantRanking`
2. Sort is already by cost (highest first)
3. Look at the top 3 🏆 assistants
4. Click "Details" to see what operations they're doing

**Action:**
- Review the prompts for expensive assistants
- Check if they can use cheaper models (gpt-4o-mini)
- Optimize vector store searches
- Reduce token usage in prompts

### 2. Compare Assistant Performance

**Goal:** See which assistants are efficient vs expensive.

**Steps:**
1. Go to `/OpenAILogs/AssistantRanking`
2. Compare "Avg Cost Per Run" column
3. Check success rates
4. Identify outliers

**Example:**
```
Assistant A: $0.050/run, 98% success
Assistant B: $0.150/run, 95% success
```
→ Assistant B is 3x more expensive! Investigate why.

### 3. Track Costs Over Time

**Goal:** Monitor if costs are increasing or decreasing.

**Steps:**
1. Go to `/OpenAILogs/AssistantDetails/{assistantId}`
2. Scroll to "Daily Cost Trend" chart
3. Look for spikes or trends

**What to look for:**
- Sudden spikes (what happened that day?)
- Increasing trend (usage growing?)
- High variability (inconsistent usage?)

### 4. Audit by Operation Type

**Goal:** Understand which operations are expensive.

**Steps:**
1. Go to `/OpenAILogs/AssistantDetails/{assistantId}`
2. Look at "Cost by Operation Type" table
3. Identify the most expensive operations

**Example:**
```
Candidate Screening: $45.30 (450 runs)
Resume Analysis: $23.10 (200 runs)
Report QC: $12.50 (100 runs)
```
→ Candidate screening costs the most. Can we optimize it?

### 5. Model Usage Analysis

**Goal:** See which models are being used and their costs.

**Steps:**
1. Go to `/OpenAILogs/AssistantDetails/{assistantId}`
2. Check "Cost by Model" table
3. Verify correct models are being used

**Example:**
```
gpt-4o: $78.50 (850 calls)
gpt-4-turbo: $12.30 (50 calls)  ← Why are we still using this?
```
→ Migration to gpt-4o incomplete! Some calls still using old model.

---

## 🔍 SQL Queries for Advanced Analysis

### Top 10 Most Expensive Assistants
```sql
SELECT TOP 10 *
FROM OpenAIAssistantCostSummary
ORDER BY TotalCostUSD DESC;
```

### Assistants with Low Success Rates
```sql
SELECT 
    AssistantId,
    TotalCostUSD,
    SuccessfulCalls,
    TotalCalls,
    CAST(SuccessfulCalls * 100.0 / TotalCalls AS DECIMAL(5,2)) as SuccessRate
FROM OpenAIAssistantCostSummary
WHERE CAST(SuccessfulCalls * 100.0 / TotalCalls AS DECIMAL(5,2)) < 90
ORDER BY TotalCostUSD DESC;
```

### Cost Per Day by Assistant
```sql
SELECT 
    AssistantId,
    TotalCostUSD,
    DaysActive,
    CAST(TotalCostUSD / NULLIF(DaysActive, 0) AS DECIMAL(10,4)) as CostPerDay
FROM OpenAIAssistantCostSummary
WHERE DaysActive > 0
ORDER BY CostPerDay DESC;
```

### Compare This Month vs Last Month
```sql
-- This month
EXEC usp_GetAssistantCostRanking 
    @StartDate = '2026-01-01', 
    @EndDate = '2026-01-31';

-- Last month
EXEC usp_GetAssistantCostRanking 
    @StartDate = '2025-12-01', 
    @EndDate = '2025-12-31';
```

---

## ⚙️ Configuration & Maintenance

### Automatic Refresh (Optional)

Create a SQL Server Agent Job to refresh summary data automatically:

```sql
-- Create a job that runs daily at 2 AM
USE msdb;
GO

EXEC dbo.sp_add_job
    @job_name = N'Refresh OpenAI Assistant Cost Summary';

EXEC sp_add_jobstep
    @job_name = N'Refresh OpenAI Assistant Cost Summary',
    @step_name = N'Execute Refresh',
    @subsystem = N'TSQL',
    @command = N'EXEC [YourDatabase].dbo.usp_RefreshAssistantCostSummary',
    @database_name = N'YourDatabase';

EXEC sp_add_schedule
    @schedule_name = N'Daily at 2 AM',
    @freq_type = 4,
    @freq_interval = 1,
    @active_start_time = 020000;

EXEC sp_attach_schedule
    @job_name = N'Refresh OpenAI Assistant Cost Summary',
    @schedule_name = N'Daily at 2 AM';

EXEC sp_add_jobserver
    @job_name = N'Refresh OpenAI Assistant Cost Summary';
```

### Manual Refresh via SQL
```sql
EXEC usp_RefreshAssistantCostSummary;
```

### Check Last Update Time
```sql
SELECT MAX(LastUpdated) as LastRefreshed
FROM OpenAIAssistantCostSummary;
```

---

## 📊 Dashboard Integration

### Add to Navigation Menu

Update `_Layout.cshtml` or your navigation:
```html
<li>
    <a asp-controller="OpenAILogs" asp-action="AssistantRanking">
        <i class="fas fa-robot"></i>
        <span>Assistant Costs</span>
    </a>
</li>
```

### Add to Dashboard

Create a widget showing top 5 expensive assistants:
```csharp
// In your dashboard controller
var topAssistants = await _openAIApiLogRepository.GetAssistantCostRankingAsync(topN: 5);
ViewBag.TopAssistants = topAssistants;
```

---

## 🎯 Best Practices

### 1. Regular Monitoring
- Check assistant ranking weekly
- Review top 3 most expensive assistants monthly
- Track trends in daily cost charts

### 2. Set Alerts
- Create alerts if an assistant exceeds expected cost
- Monitor success rates (alert if < 95%)
- Track response times (alert if > 10s)

### 3. Optimization Cycle
1. Identify expensive assistant
2. Review its operations and prompts
3. Optimize (reduce tokens, use cheaper model, etc.)
4. Measure impact (compare before/after costs)
5. Repeat

### 4. Documentation
- Add `AssistantName` column values for easy identification
- Document the purpose of each assistant
- Track optimization changes and their impact

---

## 🔧 Troubleshooting

### No Assistants Showing
**Problem:** AssistantRanking page is empty.

**Solutions:**
1. Click "Refresh Summary Data" button
2. Check if `OpenAIApiLogs` table has `AssistantId` populated:
   ```sql
   SELECT COUNT(*) FROM OpenAIApiLogs WHERE AssistantId IS NOT NULL;
   ```
3. Manually run refresh:
   ```sql
   EXEC usp_RefreshAssistantCostSummary;
   ```

### Costs Still Zero
**Problem:** TotalCostUSD showing as $0.00.

**Causes:**
1. Token usage fix not deployed yet (see OPENAI_TOKEN_USAGE_FIX.md)
2. No runs have completed successfully yet
3. Looking at historical data before fix

**Solution:**
- Deploy the token usage fix first
- Trigger some new API calls
- Refresh summary data

### Slow Performance
**Problem:** Assistant Ranking page loads slowly.

**Solutions:**
1. Add date range filter to limit data
2. Increase top N filter
3. Refresh summary table (uses cached aggregates)
4. Check index fragmentation:
   ```sql
   SELECT * FROM sys.dm_db_index_physical_stats(
       DB_ID(), 
       OBJECT_ID('OpenAIAssistantCostSummary'), 
       NULL, NULL, 'DETAILED'
   );
   ```

---

## 📈 Success Metrics

After deploying this feature, you should be able to:

✅ Answer these questions:
- Which assistant costs the most?
- What is our total OpenAI spend by assistant?
- Which operations are most expensive per assistant?
- Are any assistants underperforming (low success rate, high cost)?
- How have costs changed over time?

✅ Take these actions:
- Identify optimization opportunities
- Budget accurately by assistant
- Track ROI of individual assistants
- Make data-driven decisions about AI usage

---

## 🎉 Summary

You now have:
- ✅ Assistant-level cost tracking
- ✅ Detailed cost breakdowns
- ✅ Ranking and comparison views
- ✅ Daily trend analysis
- ✅ Performance metrics
- ✅ Easy-to-use web interface

**Next Steps:**
1. Deploy the database changes
2. Deploy the code updates
3. Refresh the summary data
4. Explore the new pages
5. Start optimizing your most expensive assistants!

---

**Questions?** Check the troubleshooting section or review the SQL scripts in `Risebird.DB/StoredProcedures/`.
