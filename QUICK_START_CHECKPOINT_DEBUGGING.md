# Quick Start: Checkpoint Debugging

**Status:** ✅ Code compiled successfully  
**Action Required:** Deploy and test

---

## Step 1: Create the Database Table ⏱️ 30 seconds

Open SQL Server Management Studio and run:

```sql
-- Execute this script: Risebird.DB\Tables\VapiWebhookCheckpoints.sql

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'VapiWebhookCheckpoints')
BEGIN
    CREATE TABLE [dbo].[VapiWebhookCheckpoints]
    (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Timestamp] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [CheckpointName] NVARCHAR(50) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [CallId] NVARCHAR(100) NULL,
        [ToolCallName] NVARCHAR(100) NULL,
        [AdditionalData] NVARCHAR(MAX) NULL,
        
        INDEX [IX_VapiWebhookCheckpoints_Timestamp] NONCLUSTERED ([Timestamp] DESC),
        INDEX [IX_VapiWebhookCheckpoints_CallId] NONCLUSTERED ([CallId]),
        INDEX [IX_VapiWebhookCheckpoints_CheckpointName] NONCLUSTERED ([CheckpointName])
    );
END
GO
```

Verify it was created:
```sql
SELECT COUNT(*) FROM VapiWebhookCheckpoints
-- Should return 0 (empty table)
```

---

## Step 2: Deploy the Application ⏱️ 5-10 minutes

Deploy the updated build to your server/environment.

---

## Step 3: Make a Test Call ⏱️ 1 minute

1. Initiate a test call through your system
2. Note the time: **____________** (write it down)
3. Let the call complete fully

---

## Step 4: Check the Checkpoints ⏱️ 1 minute

Run this query immediately after the call:

```sql
-- See the last call's flow
SELECT TOP 30
    Timestamp,
    CheckpointName,
    Message,
    CallId
FROM VapiWebhookCheckpoints
ORDER BY Timestamp DESC
```

**What to look for:**
- ✅ **Many rows appear** → Logging is working!
- ❌ **No rows appear** → Check application deployment

---

## Step 5: Find the Last Checkpoint ⏱️ 30 seconds

```sql
-- Get the complete flow for your test call
DECLARE @LatestCallId NVARCHAR(100) = (
    SELECT TOP 1 CallId 
    FROM VapiWebhookCheckpoints 
    WHERE CallId IS NOT NULL 
    ORDER BY Timestamp DESC
)

SELECT 
    ROW_NUMBER() OVER (ORDER BY Timestamp ASC) AS Step,
    Timestamp,
    CheckpointName,
    Message
FROM VapiWebhookCheckpoints
WHERE CallId = @LatestCallId
ORDER BY Timestamp ASC
```

**Read from top to bottom** - the last row is where the flow stopped.

---

## Step 6: Check if Data Saved ⏱️ 30 seconds

```sql
-- Compare checkpoints vs VapiCallLogs
DECLARE @LatestCallId NVARCHAR(100) = (
    SELECT TOP 1 CallId 
    FROM VapiWebhookCheckpoints 
    WHERE CallId IS NOT NULL 
    ORDER BY Timestamp DESC
)

-- Checkpoints summary
SELECT 
    COUNT(*) AS CheckpointCount,
    MAX(CheckpointName) AS LastCheckpoint
FROM VapiWebhookCheckpoints
WHERE CallId = @LatestCallId

-- Check VapiCallLogs
SELECT 
    Id,
    VapiCallId,
    RecipientName,
    InterviewBooked,
    BookedInterviewDateTime,
    CreatedAt
FROM VapiCallLogs
WHERE VapiCallId = @LatestCallId
```

**Expected results:**
- ✅ **CheckpointCount > 10 AND VapiCallLogs has a row** → SUCCESS! 🎉
- ❌ **CheckpointCount > 10 BUT VapiCallLogs is empty** → Flow stopped somewhere, check last checkpoint

---

## 🔍 Troubleshooting Based on Last Checkpoint

### If last checkpoint is `CHECKPOINT-1a`:
**Problem:** ToolCalls is empty  
**Cause:** Deserialization issue  
**Action:** Check webhook payload in VapiWebhookDebugLogs

### If last checkpoint is `CHECKPOINT-5`:
**Problem:** Arguments dictionary is empty  
**Cause:** JsonProperty mapping issue  
**Action:** Run this to see the raw arguments:
```sql
SELECT AdditionalData 
FROM VapiWebhookCheckpoints 
WHERE CheckpointName = 'CHECKPOINT-5'
ORDER BY Timestamp DESC
```

### If last checkpoint is `CHECKPOINT-7`:
**Problem:** Data extraction issue  
**Action:** Look at the Message field - check for empty/null values

### If last checkpoint is `CHECKPOINT-8-SKIPPED`:
**Problem:** DateTime parsing failed  
**Action:** Check CHECKPOINT-7 message for the date/time format

### If last checkpoint is `CHECKPOINT-9-SKIPPED`:
**Problem:** Candidate not confirmed  
**Action:** Check CHECKPOINT-7 to see candidateConfirmed value

### If last checkpoint is `CHECKPOINT-14`:
**Problem:** Repository call not completing  
**Action:** Check for REPO-A, REPO-B checkpoints

### If last checkpoint is `REPO-B`:
**Problem:** SQL INSERT not completing  
**Action:** Check SQL Server logs, table constraints

### If last checkpoint is `CHECKPOINT-15-SUCCESS`:
**Problem:** None - it worked!  
**Action:** Verify VapiCallLogs table has the record

---

## 📊 One Query to Rule Them All

Run this for a complete diagnostic:

```sql
DECLARE @LatestCallId NVARCHAR(100) = (
    SELECT TOP 1 CallId 
    FROM VapiWebhookCheckpoints 
    WHERE CallId IS NOT NULL 
    ORDER BY Timestamp DESC
)

SELECT 
    '=== CHECKPOINT FLOW ===' AS Section,
    ROW_NUMBER() OVER (ORDER BY Timestamp ASC) AS Step,
    CheckpointName,
    Message,
    LEFT(ISNULL(AdditionalData, ''), 100) AS DataPreview
FROM VapiWebhookCheckpoints
WHERE CallId = @LatestCallId

UNION ALL

SELECT 
    '=== VAPI CALL LOG ===' AS Section,
    Id AS Step,
    'VapiCallLog Entry' AS CheckpointName,
    CONCAT('RecipientName: ', RecipientName, ', InterviewBooked: ', CAST(InterviewBooked AS NVARCHAR)) AS Message,
    CONCAT('BookedDateTime: ', CAST(BookedInterviewDateTime AS NVARCHAR)) AS DataPreview
FROM VapiCallLogs
WHERE VapiCallId = @LatestCallId

ORDER BY Section DESC, Step ASC
```

---

## 🎯 What to Report

After running the test, copy and paste these results:

1. **Last checkpoint name:**
```sql
SELECT TOP 1 CheckpointName 
FROM VapiWebhookCheckpoints 
ORDER BY Timestamp DESC
```

2. **Last checkpoint message:**
```sql
SELECT TOP 1 Message 
FROM VapiWebhookCheckpoints 
ORDER BY Timestamp DESC
```

3. **Data in VapiCallLogs:**
```sql
SELECT COUNT(*) 
FROM VapiCallLogs 
WHERE CreatedAt > DATEADD(MINUTE, -10, GETUTCDATE())
```

Share these three pieces of information, and we'll know exactly what's happening! 🔍

---

## ✅ Success Looks Like This

```
Step  CheckpointName           Message
----  -------------------      -------
1     WEBHOOK-START           Processing tool-calls - Tool count: 1
2     WEBHOOK-BEFORE-HANDLER  About to call HandleInterviewSlotSubmission
3     CHECKPOINT-1            HandleInterviewSlotSubmission method entry
4     CHECKPOINT-2            ToolCalls found - Count: 1
5     CHECKPOINT-3            Examining tool call - Name: submitInterviewSlot
6     CHECKPOINT-4            Found submitInterviewSlot tool call
7     CHECKPOINT-5            Arguments - Args null: False, Args count: 5
8     CHECKPOINT-6            CallId validated: 019c...
9     CHECKPOINT-7            Data extracted - Name: 'Harsh Jajoo', Date: '2026-02-21'...
10    CHECKPOINT-8            DateTime parsed successfully - 2/21/2026 10:00:00 AM
11    CHECKPOINT-9            Candidate confirmed - proceeding to find/create call log
12    CHECKPOINT-10           Looking for existing call log
13    REPO-D                  GetCallLogByVapiCallIdAsync called...
14    REPO-E                  Query executed - Result found: False
15    CHECKPOINT-11           Call log lookup result - Found: False
16    CHECKPOINT-12           Call log not found - likely initiated from Vapi dashboard
17    CHECKPOINT-13           About to save to database...
18    CHECKPOINT-14           Calling SaveCallLogAsync for new call log
19    REPO-A                  SaveCallLogAsync called...
20    REPO-B                  Connection created, about to execute INSERT
21    REPO-C-SUCCESS          INSERT executed successfully - New ID: 123
22    CHECKPOINT-15-SUCCESS   NEW call log created - ID: 123...
23    WEBHOOK-AFTER-HANDLER   HandleInterviewSlotSubmission completed
```

**AND** a matching row in `VapiCallLogs` with `Id = 123`

---

## 📞 Need Help?

Share:
1. Last checkpoint name
2. Last checkpoint message  
3. Count of checkpoints for that CallId
4. Whether VapiCallLogs has an entry

That's all we need to diagnose the issue! 🚀
