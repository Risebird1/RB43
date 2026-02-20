# 🔧 Vapi Webhook Debugging - START HERE

## ✅ What Was Done

I've added a comprehensive debugging system to track every webhook request from Vapi and identify exactly where your data is getting lost. The code has been built successfully with only warnings (no errors).

## 🚀 Quick Start (5 Minutes)

### Step 1: Create the Debug Table (MUST DO FIRST!)

Open **SQL Server Management Studio** and run this file:
```
SETUP_DEBUG_TABLE.sql
```

Or copy/paste this SQL:

```sql
CREATE TABLE [dbo].[VapiWebhookDebugLogs]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestTimestamp] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [WebhookType] NVARCHAR(100) NULL,
    [CallId] NVARCHAR(100) NULL,
    [AssistantId] NVARCHAR(100) NULL,
    [ToolCallName] NVARCHAR(100) NULL,
    [RawPayload] NVARCHAR(MAX) NULL,
    [ProcessingStatus] NVARCHAR(50) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CallLogFound] BIT NULL,
    [CallLogId] INT NULL,
    [DataSaved] BIT NULL,
    
    INDEX [IX_VapiWebhookDebugLogs_CallId] NONCLUSTERED ([CallId]),
    INDEX [IX_VapiWebhookDebugLogs_RequestTimestamp] NONCLUSTERED ([RequestTimestamp] DESC),
    INDEX [IX_VapiWebhookDebugLogs_ToolCallName] NONCLUSTERED ([ToolCallName])
);
```

### Step 2: Restart Your Application

1. **STOP** your web application
2. **START** it again (this loads the new debugging code)

### Step 3: Test Webhook Endpoint

Open your browser and visit:
```
https://portal.risebird.io/VapiInterviewCall/WebhookTest
```

**Expected Result:**
```json
{
  "status": "ok",
  "message": "Vapi webhook endpoint is accessible",
  "timestamp": "...",
  "url": "https://portal.risebird.io/VapiInterviewCall/Webhook"
}
```

❌ **If you get 404 or error:**
- Your application is not running
- Or there's a routing issue

✅ **If you see the JSON response:**
- Webhook endpoint is working!
- Continue to Step 4

### Step 4: Make a Test Call

Use Vapi dashboard to make a test call:
1. Go to Vapi dashboard
2. Select your assistant (ID: `5a7c4ecb-fcba-4fa6-bcf1-48842d742144`)
3. Make a test call
4. Complete the call with a confirmed interview slot

### Step 5: Check Debug Logs

Open your browser and visit:
```
https://portal.risebird.io/VapiInterviewCall/WebhookDebugLogs?count=20
```

This will show you ALL webhook requests received.

## 📊 What to Look For

### ✅ SUCCESS - Everything Working
```json
{
  "id": 1,
  "requestTimestamp": "2026-01-29T...",
  "webhookType": "tool-calls",
  "callId": "abc123",
  "processingStatus": "success",
  "callLogFound": true,
  "callLogId": 42,
  "dataSaved": true,
  "errorMessage": null
}
```

**Meaning:** Data was saved! Check VapiCallLogs table with ID 42.

### ❌ NO WEBHOOKS - Vapi Not Sending
```json
{
  "success": true,
  "logs": []
}
```

**Problem:** Vapi is not sending webhooks to your server.

**Fix:**
1. Go to Vapi dashboard
2. Find your assistant
3. Go to "Tools" section
4. Check `submitInterviewSlot` tool
5. Verify "Server URL" is: `https://portal.risebird.io/VapiInterviewCall/Webhook`
6. Save and try again

### ⚠️ WEBHOOKS RECEIVED BUT ERROR
```json
{
  "processingStatus": "error",
  "errorMessage": "Invalid column name 'VapiCallId'",
  ...
}
```

**Problem:** Database issue (table structure problem).

**Fix:** Check the error message and:
- Run migration scripts if columns are missing
- Verify VapiCallLogs table structure matches the schema

### ⚠️ DATA VALIDATION FAILED
```json
{
  "processingStatus": "skipped",
  "errorMessage": "Failed to parse date/time: ...",
  ...
}
```

**Problem:** Assistant collecting wrong data format.

**Fix:**
- Date must be: YYYY-MM-DD (e.g., "2026-01-30")
- Time must be: HH:MM AM/PM (e.g., "02:30 PM")
- Timezone should be: IST
- Candidate must explicitly confirm: "yes"

## 🔍 Database Queries

### Check if webhooks are being logged:
```sql
SELECT COUNT(*) FROM VapiWebhookDebugLogs;
```

### View recent webhook attempts:
```sql
SELECT TOP 20 
    RequestTimestamp,
    WebhookType,
    CallId,
    ProcessingStatus,
    DataSaved,
    ErrorMessage
FROM VapiWebhookDebugLogs
ORDER BY RequestTimestamp DESC;
```

### Check if data was saved to VapiCallLogs:
```sql
SELECT * FROM VapiCallLogs
ORDER BY CreatedAt DESC;
```

### Find webhooks for a specific call:
```sql
-- Replace 'your-call-id' with actual CallId from debug logs
SELECT * FROM VapiWebhookDebugLogs 
WHERE CallId = 'your-call-id'
ORDER BY RequestTimestamp;
```

### View full webhook payload:
```sql
SELECT CallId, RawPayload, ErrorMessage
FROM VapiWebhookDebugLogs 
WHERE ProcessingStatus = 'error'
ORDER BY RequestTimestamp DESC;
```

## 🎯 What Changed in the Code

### 1. New Debug Table
- `VapiWebhookDebugLogs` - Logs EVERY webhook request

### 2. Enhanced Webhook Controller
- Logs every incoming webhook with full payload
- Tracks processing status at each step
- Captures errors with full details
- Uses ✅ SUCCESS, ❌ FAILED, ⚠️ SKIPPED emojis in logs

### 3. New Test Endpoints
- `GET /VapiInterviewCall/WebhookTest` - Test webhook connectivity
- `GET /VapiInterviewCall/WebhookDebugLogs` - View debug logs

### 4. Better Error Handling
- Try-catch around database saves
- Detailed logging at every step
- Creates call log if doesn't exist
- Updates debug table with results

## 📞 Report Back With

After completing Steps 1-5 above, please share:

1. **WebhookTest result** (Step 3)
2. **WebhookDebugLogs result** (Step 5)
3. **Count of records in VapiCallLogs table**
4. **Any errors you see**

This will tell me exactly what's wrong and how to fix it.

## 📚 Additional Documentation

- `VAPI_DEBUGGING_GUIDE.md` - Detailed debugging instructions
- `QUICK_FIX_STEPS.md` - Quick troubleshooting guide
- `SETUP_DEBUG_TABLE.sql` - Database setup script

## 🆘 Common Issues

### Issue: "Table 'VapiWebhookDebugLogs' does not exist"
**Fix:** Run Step 1 (create the debug table)

### Issue: WebhookTest returns 404
**Fix:** Make sure application is running and URL is correct

### Issue: WebhookDebugLogs shows empty array
**Fix:** Vapi is not sending webhooks - check webhook URL in Vapi dashboard

### Issue: processingStatus = "error"
**Fix:** Check errorMessage field and database logs

---

**🎯 Your Next Action:** Complete Steps 1-5 above and report the results!
