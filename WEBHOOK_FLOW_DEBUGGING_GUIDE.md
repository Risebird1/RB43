# Webhook Flow Debugging Guide - VapiCallLogs Not Saving

**Date:** February 20, 2026  
**Build Status:** ✅ Compiled successfully

---

## 🔍 Enhanced Logging Checkpoints

I've added 19 comprehensive logging checkpoints throughout the webhook processing flow. Each checkpoint logs to the application error logger so you can trace exactly where the execution stops.

### Checkpoint Map

#### **WEBHOOK LEVEL (Lines 236-241 in VapiInterviewCallController.cs)**
- `🔍 WEBHOOK: Processing tool-calls` - Confirms tool-calls message type received
- `🔍 WEBHOOK: About to call HandleInterviewSlotSubmission` - Right before calling the handler
- `🔍 WEBHOOK: HandleInterviewSlotSubmission completed` - After handler completes

#### **HANDLEINTERVIEWSLOTSUBMISSION METHOD**

| Checkpoint | What It Means | Why It Matters |
|------------|---------------|----------------|
| **CHECKPOINT 1** | Method entry | Confirms HandleInterviewSlotSubmission was called |
| **CHECKPOINT 1a** | ToolCalls null/empty | If you see this, ToolCalls deserialization failed |
| **CHECKPOINT 2** | ToolCalls found | Shows count and function names |
| **CHECKPOINT 3** | Examining each tool call | Logs each tool name |
| **CHECKPOINT 4** | Found submitInterviewSlot | Confirms the right tool was called |
| **CHECKPOINT 5** | Arguments check | Shows if Arguments dictionary is populated |
| **CHECKPOINT 6** | CallId validated | Confirms CallId is present |
| **CHECKPOINT 7** | Data extracted | Shows all extracted values (name, date, time, etc.) |
| **CHECKPOINT 8** | DateTime parsed | Confirms date/time parsing succeeded |
| **CHECKPOINT 9** | Candidate confirmed | Confirms candidateConfirmed = true |
| **CHECKPOINT 10** | Looking for call log | About to query database |
| **CHECKPOINT 11** | Call log lookup result | Whether existing log was found |
| **CHECKPOINT 12** | Call log not found | Call initiated from Vapi dashboard |
| **CHECKPOINT 13** | About to save to database | Last checkpoint before repository call |
| **CHECKPOINT 14** | Calling SaveCallLogAsync | About to insert new record |
| **CHECKPOINT 15** | Save successful (new) | New record created successfully |
| **CHECKPOINT 16** | Calling UpdateCallLogAsync | About to update existing record |
| **CHECKPOINT 17** | Update successful | Existing record updated successfully |
| **CHECKPOINT 18** | Database save failed | Exception during save operation |
| **CHECKPOINT 19** | Method-level exception | Exception in HandleInterviewSlotSubmission |

#### **REPOSITORY LEVEL (VapiCallRepository.cs)**

| Checkpoint | What It Means |
|------------|---------------|
| **REPOSITORY CHECKPOINT A** | SaveCallLogAsync called with parameters |
| **REPOSITORY CHECKPOINT B** | SQL connection created, about to execute |
| **REPOSITORY CHECKPOINT C** | INSERT executed successfully with new ID |
| **REPOSITORY CHECKPOINT D** | GetCallLogByVapiCallIdAsync called |
| **REPOSITORY CHECKPOINT E** | Query executed, shows if record found |
| **REPOSITORY ERROR** | Exception in repository method |

---

## 🎯 How to Debug Your Next Call

### Step 1: Make a Test Call
Run another test call through your system.

### Step 2: Check Application Error Logs

**Location:** Azure Table Storage - Table name: `Risebirdlogs`

Access via:
- Azure Portal: Storage Account → Tables → Risebirdlogs
- Or use Azure Storage Explorer
- Filter by: Last 30 minutes, look for LogMessage containing "CHECKPOINT"

**What to look for:**
- Sort by Timestamp (descending) to get the latest entries
- Look for entries with LogMessage containing "🔍 CHECKPOINT" or "❌ CHECKPOINT"
- The PartitionKey uses reverse ticks, so newest entries are at the top

### Step 3: Find the Last Checkpoint Reached
Look through the log entries in order. The checkpoints will tell you exactly where the flow stopped.

#### **Scenario A: Flow stops at CHECKPOINT 1a**
**Meaning:** ToolCalls is null or empty
**Cause:** Deserialization issue - the `[JsonProperty("arguments")]` fix didn't work
**Check:** 
- Verify the build was deployed
- Check RawPayload in VapiWebhookDebugLogs to see the actual JSON structure
- Verify VapiToolCall.Function.Arguments is being populated

#### **Scenario B: Flow stops at CHECKPOINT 5 (Arguments null or empty)**
**Meaning:** Tool call found, but Arguments dictionary is empty
**Cause:** Deserialization still not working properly
**Fix:** Need to investigate the JSON structure more carefully

#### **Scenario C: Flow stops at CHECKPOINT 7 with empty values**
**Meaning:** Arguments populated but values are empty/null
**Cause:** Key names don't match (e.g., "candidateName" vs "candidate_name")
**Check:** Compare the keys in Arguments with expected keys

#### **Scenario D: Flow stops at CHECKPOINT 8 (DateTime parse failed)**
**Meaning:** Date/time format is invalid
**Cause:** AI sent date in wrong format or missing AM/PM
**Check:** The date and time values in CHECKPOINT 7

#### **Scenario E: Flow stops at CHECKPOINT 9 (Not confirmed)**
**Meaning:** candidateConfirmed = false
**Cause:** AI didn't get explicit confirmation or set the flag incorrectly
**Check:** Review the call transcript

#### **Scenario F: Reaches CHECKPOINT 13 but no further checkpoints**
**Meaning:** Code execution stopped before calling repository
**Cause:** Likely an exception between 13 and 14
**Check:** Look for any exception logs

#### **Scenario G: Reaches CHECKPOINT 14 but no CHECKPOINT 15**
**Meaning:** SaveCallLogAsync was called but never returned
**Cause:** Database operation hanging or failing silently
**Check:** 
- Database connection string
- SQL Server logs
- Table permissions
- Look for REPOSITORY CHECKPOINT B and C

#### **Scenario H: Reaches CHECKPOINT 18 or 19**
**Meaning:** Exception occurred during save
**Cause:** Check the exception message in the log
**Common causes:**
- SQL constraint violation
- Connection timeout
- Permission denied
- Invalid data type conversion

---

## 📊 Expected Successful Flow

When everything works correctly, you should see this sequence:

```
🔍 WEBHOOK: Processing tool-calls - CallId: xxx, Tool count: 1
🔍 WEBHOOK: About to call HandleInterviewSlotSubmission for CallId: xxx
🔍 CHECKPOINT 1: HandleInterviewSlotSubmission called
🔍 CHECKPOINT 2: ToolCalls found - Count: 1, Tool names: submitInterviewSlot
🔍 CHECKPOINT 3: Examining tool call - Name: submitInterviewSlot
🔍 CHECKPOINT 4: Found submitInterviewSlot tool call
🔍 CHECKPOINT 5: Arguments - CallId: xxx, Args null: False, Args count: 5, Args JSON: {...}
🔍 CHECKPOINT 6: CallId validated: xxx
🔍 CHECKPOINT 7: Data extracted - Name: 'Harsh Jajoo', Date: '2026-02-21', Time: '10:00 AM', TZ: 'IST', Confirmed: True
🔍 CHECKPOINT 8: DateTime parsed successfully - 2026-02-21 10:00:00 AM
🔍 CHECKPOINT 9: Candidate confirmed - proceeding to find/create call log
🔍 CHECKPOINT 10: Looking for existing call log with CallId: xxx
🔍 REPOSITORY CHECKPOINT D: GetCallLogByVapiCallIdAsync called - Looking for VapiCallId: xxx
🔍 REPOSITORY CHECKPOINT E: Query executed - Result found: False, Result ID: 0
🔍 CHECKPOINT 11: Call log lookup result - Found: False, Will create new: True
⚠️ CHECKPOINT 12: Call log not found for CallId: xxx. This usually means the call was initiated from Vapi dashboard...
🔍 CHECKPOINT 13: About to save to database - IsNew: True, CallId: xxx, RecipientPhone: +91xxx, RecipientName: Harsh Jajoo
🔍 CHECKPOINT 14: Calling SaveCallLogAsync for new call log
🔍 REPOSITORY CHECKPOINT A: SaveCallLogAsync called - VapiCallId: xxx, RecipientPhone: +91xxx, RecipientName: Harsh Jajoo
🔍 REPOSITORY CHECKPOINT B: Connection created, about to execute INSERT
🔍 REPOSITORY CHECKPOINT C: INSERT executed successfully - New ID: 1
✅ CHECKPOINT 15: SUCCESS - NEW call log created - ID: 1, CallId: xxx, DateTime: 2026-02-21 10:00:00 AM, Candidate: Harsh Jajoo
🔍 WEBHOOK: HandleInterviewSlotSubmission completed for CallId: xxx
```

---

## 🔧 Quick Diagnostic SQL Queries

### Check Latest Webhook Debug Logs
```sql
SELECT TOP 10 
    Id,
    RequestTimestamp,
    WebhookType,
    CallId,
    ProcessingStatus,
    ErrorMessage,
    CallLogFound,
    CallLogId,
    DataSaved
FROM VapiWebhookDebugLogs
ORDER BY RequestTimestamp DESC
```

### Check VapiCallLogs
```sql
SELECT TOP 10 *
FROM VapiCallLogs
ORDER BY CreatedAt DESC
```

### Check Application Error Logs for Checkpoints
```sql
-- Adjust table/column names based on your error logging table
SELECT TOP 50 
    LogTime,
    Message
FROM ApplicationErrorLog
WHERE Message LIKE '%CHECKPOINT%'
    AND LogTime > DATEADD(MINUTE, -30, GETUTCDATE())
ORDER BY LogTime ASC
```

---

## 🚨 What to Do Next

1. **Deploy the updated build** to your server/environment

2. **Make a test call** and note the exact time

3. **Check the error logs** for the checkpoints around that time

4. **Identify the last checkpoint reached** and report back which one it was

5. **Share the specific checkpoint data** - this will tell us exactly where the flow is breaking

---

## 💡 Most Likely Issues

Based on the symptoms (webhook logs present but no VapiCallLogs entry):

### 1. **Early Return at CHECKPOINT 1a** (Most Likely)
If ToolCalls is null/empty, the method returns immediately. This would happen if the `[JsonProperty("arguments")]` fix didn't take effect.

**What to check:** CHECKPOINT 1a in logs

### 2. **Validation Failure** (Likely)
One of the validation checks (empty name, unparseable date, not confirmed) is causing an early return.

**What to check:** CHECKPOINTs 7, 8, 9 to see which validation fails

### 3. **Silent Exception** (Possible)
An exception is being caught and logged but not preventing the method from returning normally.

**What to check:** CHECKPOINT 18 or 19 for exceptions

### 4. **Repository Never Called** (Possible)
Code path skips the repository call entirely.

**What to check:** CHECKPOINT 13, 14 should appear if repository is being called

---

## 📝 Reporting Template

When reporting results, please provide:

1. **Last checkpoint reached:** (e.g., "CHECKPOINT 7")
2. **Checkpoint data:** (copy the full log message)
3. **Any error/warning checkpoints:** (e.g., CHECKPOINT 1a, 18, or 19)
4. **WebhookDebugLog entry:** (ProcessingStatus, ErrorMessage, DataSaved values)
5. **Time of test call:** (so I can correlate with logs)

This will allow me to pinpoint the exact issue immediately.
