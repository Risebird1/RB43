# Database Checkpoint Debugging Guide

**Date:** February 20, 2026  
**Build Status:** ✅ Compiled successfully  
**Logging Location:** SQL Database table `VapiWebhookCheckpoints`

---

## 📊 New Database Table Created

### Table: `VapiWebhookCheckpoints`

```sql
CREATE TABLE [dbo].[VapiWebhookCheckpoints]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Timestamp] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [CheckpointName] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [CallId] NVARCHAR(100) NULL,
    [ToolCallName] NVARCHAR(100) NULL,
    [AdditionalData] NVARCHAR(MAX) NULL
)
```

**Indexes:**
- `IX_VapiWebhookCheckpoints_Timestamp` (DESC) - for time-based queries
- `IX_VapiWebhookCheckpoints_CallId` - for call-specific queries
- `IX_VapiWebhookCheckpoints_CheckpointName` - for filtering by checkpoint

---

## 🔍 Checkpoint Reference

### Webhook Level Checkpoints
| Checkpoint Name | Meaning |
|----------------|---------|
| **WEBHOOK-START** | Tool-calls message received, shows tool count |
| **WEBHOOK-BEFORE-HANDLER** | About to call HandleInterviewSlotSubmission |
| **WEBHOOK-AFTER-HANDLER** | HandleInterviewSlotSubmission completed |

### HandleInterviewSlotSubmission Checkpoints
| Checkpoint Name | Meaning | What to Check |
|----------------|---------|---------------|
| **CHECKPOINT-1** | Method entry | Confirms method was called |
| **CHECKPOINT-1a** | ToolCalls null/empty | Deserialization issue if you see this |
| **CHECKPOINT-2** | ToolCalls found | Check AdditionalData for tool names |
| **CHECKPOINT-3** | Examining each tool call | |
| **CHECKPOINT-4** | Found submitInterviewSlot | Confirms correct tool found |
| **CHECKPOINT-5** | Arguments extracted | Check AdditionalData for full JSON |
| **CHECKPOINT-6** | CallId validated | |
| **CHECKPOINT-6-ERROR** | CallId is null/empty | |
| **CHECKPOINT-7** | Data extracted | Shows all extracted parameters |
| **CHECKPOINT-8** | DateTime parsed successfully | |
| **CHECKPOINT-8-SKIPPED** | DateTime parsing failed | Check date/time format |
| **CHECKPOINT-9** | Candidate confirmed | |
| **CHECKPOINT-9-SKIPPED** | Candidate not confirmed | |
| **CHECKPOINT-10** | Looking for existing call log | |
| **CHECKPOINT-11** | Call log lookup result | Shows if existing log found |
| **CHECKPOINT-12** | Call log not found | Call from Vapi dashboard |
| **CHECKPOINT-13** | About to save to database | Last checkpoint before repository |
| **CHECKPOINT-14** | Calling SaveCallLogAsync | |
| **CHECKPOINT-15-SUCCESS** | New call log created | Shows new ID |
| **CHECKPOINT-16** | Calling UpdateCallLogAsync | |
| **CHECKPOINT-17-SUCCESS** | Existing call log updated | |
| **CHECKPOINT-18-ERROR** | Database save failed | Check AdditionalData for stack trace |
| **CHECKPOINT-19-EXCEPTION** | Method-level exception | Check AdditionalData for full error |

### Repository Checkpoints
| Checkpoint Name | Meaning |
|----------------|---------|
| **REPO-A** | SaveCallLogAsync called with parameters |
| **REPO-B** | SQL connection created, about to INSERT |
| **REPO-C-SUCCESS** | INSERT executed successfully, shows new ID |
| **REPO-D** | GetCallLogByVapiCallIdAsync called |
| **REPO-E** | SELECT query executed, shows if record found |
| **REPO-ERROR** | Repository exception occurred |

---

## 🎯 SQL Queries for Debugging

### 1. View Latest Checkpoints (All Calls)
```sql
SELECT TOP 50 
    Id,
    Timestamp,
    CheckpointName,
    Message,
    CallId,
    ToolCallName
FROM VapiWebhookCheckpoints
ORDER BY Timestamp DESC
```

### 2. View Checkpoints for Specific CallId
```sql
DECLARE @CallId NVARCHAR(100) = 'YOUR_CALL_ID_HERE' -- Replace with actual CallId

SELECT 
    Id,
    Timestamp,
    CheckpointName,
    Message,
    CallId,
    ToolCallName,
    AdditionalData
FROM VapiWebhookCheckpoints
WHERE CallId = @CallId
ORDER BY Timestamp ASC
```

### 3. View Last Call's Complete Flow
```sql
-- Get the most recent CallId
DECLARE @LatestCallId NVARCHAR(100) = (
    SELECT TOP 1 CallId 
    FROM VapiWebhookCheckpoints 
    WHERE CallId IS NOT NULL 
    ORDER BY Timestamp DESC
)

-- Show all checkpoints for that call
SELECT 
    ROW_NUMBER() OVER (ORDER BY Timestamp ASC) AS Step,
    Timestamp,
    CheckpointName,
    Message,
    ToolCallName,
    AdditionalData
FROM VapiWebhookCheckpoints
WHERE CallId = @LatestCallId
ORDER BY Timestamp ASC
```

### 4. Find Where Flow Stopped (Last Checkpoint Reached)
```sql
-- Get the most recent CallId
DECLARE @LatestCallId NVARCHAR(100) = (
    SELECT TOP 1 CallId 
    FROM VapiWebhookCheckpoints 
    WHERE CallId IS NOT NULL 
    ORDER BY Timestamp DESC
)

-- Show last checkpoint
SELECT TOP 1
    Timestamp,
    CheckpointName,
    Message,
    AdditionalData
FROM VapiWebhookCheckpoints
WHERE CallId = @LatestCallId
ORDER BY Timestamp DESC
```

### 5. View All Error Checkpoints
```sql
SELECT 
    Timestamp,
    CheckpointName,
    Message,
    CallId,
    AdditionalData
FROM VapiWebhookCheckpoints
WHERE CheckpointName LIKE '%-ERROR' 
   OR CheckpointName LIKE '%-EXCEPTION'
   OR CheckpointName LIKE '%-SKIPPED'
ORDER BY Timestamp DESC
```

### 6. View Checkpoints from Last N Minutes
```sql
DECLARE @MinutesAgo INT = 10 -- Change as needed

SELECT 
    Timestamp,
    CheckpointName,
    Message,
    CallId,
    ToolCallName
FROM VapiWebhookCheckpoints
WHERE Timestamp > DATEADD(MINUTE, -@MinutesAgo, GETUTCDATE())
ORDER BY Timestamp DESC
```

### 7. Compare with VapiCallLogs Table
```sql
-- See which calls have checkpoints but no VapiCallLog entry
SELECT DISTINCT
    c.CallId,
    c.Timestamp AS LastCheckpointTime,
    c.CheckpointName AS LastCheckpoint,
    v.Id AS VapiCallLogId
FROM VapiWebhookCheckpoints c
LEFT JOIN VapiCallLogs v ON c.CallId = v.VapiCallId
WHERE c.CallId IS NOT NULL
    AND c.Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY c.Timestamp DESC
```

### 8. Full Diagnostic Query
```sql
-- Comprehensive view of recent webhook processing
SELECT 
    c.CallId,
    COUNT(*) AS CheckpointCount,
    MIN(c.Timestamp) AS FirstCheckpoint,
    MAX(c.Timestamp) AS LastCheckpoint,
    MAX(c.CheckpointName) AS LastCheckpointName,
    MAX(c.Message) AS LastMessage,
    CASE WHEN v.Id IS NOT NULL THEN 'YES' ELSE 'NO' END AS SavedToVapiCallLogs,
    v.Id AS VapiCallLogId
FROM VapiWebhookCheckpoints c
LEFT JOIN VapiCallLogs v ON c.CallId = v.VapiCallId
WHERE c.CallId IS NOT NULL
    AND c.Timestamp > DATEADD(MINUTE, -30, GETUTCDATE())
GROUP BY c.CallId, v.Id
ORDER BY MAX(c.Timestamp) DESC
```

---

## 🚀 Deployment Steps

### 1. Create the Table
Run the SQL script to create the table:
```sql
-- Run: Risebird.DB\Tables\VapiWebhookCheckpoints.sql
```

### 2. Deploy the Updated Build
Deploy the compiled application with the new checkpoint logging.

### 3. Make a Test Call
Initiate a test call and note the time.

### 4. Query the Checkpoints
Use Query #3 above to see the complete flow for your test call.

---

## 🔎 Interpreting Results

### Scenario A: No Checkpoints at All
**Meaning:** Webhook endpoint not being called
**Check:** 
- Webhook URL configuration in Vapi dashboard
- Application is running and accessible

### Scenario B: Only WEBHOOK-START, no further checkpoints
**Meaning:** HandleInterviewSlotSubmission not being called
**Check:** 
- Webhook processing switch statement
- Tool-calls message type

### Scenario C: Stops at CHECKPOINT-1a
**Meaning:** ToolCalls is null or empty
**Cause:** Deserialization issue with `VapiToolFunction.Arguments`
**Action:** Check the `AdditionalData` field to see why ToolCalls is empty

### Scenario D: Stops at CHECKPOINT-5
**Meaning:** Arguments dictionary is empty/null
**Check:** The `AdditionalData` field for the actual JSON structure

### Scenario E: Stops at CHECKPOINT-7
**Meaning:** Data extraction issue
**Check:** The extracted values in the Message field - look for empty/null values

### Scenario F: Stops at CHECKPOINT-8-SKIPPED
**Meaning:** DateTime parsing failed
**Check:** The date and time format in CHECKPOINT-7

### Scenario G: Stops at CHECKPOINT-9-SKIPPED
**Meaning:** Candidate not confirmed
**Check:** The value of `candidateConfirmed` in CHECKPOINT-7

### Scenario H: Reaches CHECKPOINT-14 but no REPO-B
**Meaning:** Repository method call failed or hung
**Check:** Look for REPO-ERROR checkpoint

### Scenario I: Reaches REPO-B but no REPO-C-SUCCESS
**Meaning:** SQL INSERT operation failed or hung
**Check:** 
- Database connection
- SQL Server logs
- Table constraints
- Look for REPO-ERROR

### Scenario J: Reaches CHECKPOINT-15-SUCCESS
**Meaning:** Everything worked!
**Check:** VapiCallLogs table should have the new record

---

## 📝 After Running a Test Call

Run this query to get a summary:

```sql
-- Replace with your test call time
DECLARE @TestTime DATETIME = '2026-02-20 12:30:00' -- Adjust to your test time

SELECT 
    c.Timestamp,
    c.CheckpointName,
    c.Message,
    c.CallId,
    CASE 
        WHEN c.AdditionalData IS NOT NULL 
        THEN LEFT(c.AdditionalData, 200) + '...'
        ELSE NULL 
    END AS DataPreview
FROM VapiWebhookCheckpoints c
WHERE c.Timestamp >= @TestTime
ORDER BY c.Timestamp ASC
```

Then report back:
1. **Last checkpoint name** (e.g., "CHECKPOINT-7")
2. **Message from that checkpoint**
3. **Any ERROR or SKIPPED checkpoints**
4. **Whether REPO-C-SUCCESS appeared**

---

## 🧹 Cleanup Query (Optional)

To clear old checkpoint data:

```sql
-- Delete checkpoints older than 24 hours
DELETE FROM VapiWebhookCheckpoints
WHERE Timestamp < DATEADD(HOUR, -24, GETUTCDATE())

-- Or delete all checkpoints
TRUNCATE TABLE VapiWebhookCheckpoints
```

---

## ✅ Success Criteria

When everything works, you should see this sequence for a CallId:

1. WEBHOOK-START
2. WEBHOOK-BEFORE-HANDLER
3. CHECKPOINT-1
4. CHECKPOINT-2
5. CHECKPOINT-3
6. CHECKPOINT-4
7. CHECKPOINT-5 (with JSON in AdditionalData)
8. CHECKPOINT-6
9. CHECKPOINT-7 (with all extracted values)
10. CHECKPOINT-8
11. CHECKPOINT-9
12. CHECKPOINT-10
13. REPO-D
14. REPO-E (Found: False)
15. CHECKPOINT-11
16. CHECKPOINT-12
17. CHECKPOINT-13
18. CHECKPOINT-14
19. REPO-A
20. REPO-B
21. REPO-C-SUCCESS (with new ID)
22. CHECKPOINT-15-SUCCESS
23. WEBHOOK-AFTER-HANDLER

**AND** a new row in `VapiCallLogs` table with matching `VapiCallId`.

---

## 🎯 Quick Test Query

After your test call, run this to see if it worked:

```sql
SELECT TOP 1
    'Checkpoints' AS Source,
    COUNT(*) AS Count,
    MAX(CheckpointName) AS LastCheckpoint,
    CallId
FROM VapiWebhookCheckpoints
WHERE Timestamp > DATEADD(MINUTE, -5, GETUTCDATE())
    AND CallId IS NOT NULL
GROUP BY CallId

UNION ALL

SELECT 
    'VapiCallLogs' AS Source,
    COUNT(*) AS Count,
    'N/A' AS LastCheckpoint,
    VapiCallId AS CallId
FROM VapiCallLogs
WHERE CreatedAt > DATEADD(MINUTE, -5, GETUTCDATE())
GROUP BY VapiCallId

ORDER BY CallId DESC
```

If you see one row with "Checkpoints" and one row with "VapiCallLogs" for the same CallId, it worked! 🎉
