# Quick Fix Steps - Vapi Webhook Not Saving Data

## 🚀 Immediate Actions (5 minutes)

### Step 1: Create Debug Table
Open **SQL Server Management Studio** and run:

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

### Step 2: Rebuild Application
```powershell
cd C:\Users\harsh\source\repos\Risebird
dotnet build
```

### Step 3: Restart Your Application
- **Stop** your web application
- **Start** it again

### Step 4: Test Webhook Connectivity
Open browser and go to:
```
https://portal.risebird.io/VapiInterviewCall/WebhookTest
```

**Expected response:**
```json
{
  "status": "ok",
  "message": "Vapi webhook endpoint is accessible",
  ...
}
```

❌ **If you get 404 or error** → Application not running or routing issue
✅ **If you get the JSON response** → Webhook endpoint is working

### Step 5: Make a Test Call
Make a call through Vapi and let it complete with a confirmed interview slot.

### Step 6: Check Debug Logs
Open browser and go to:
```
https://portal.risebird.io/VapiInterviewCall/WebhookDebugLogs?count=20
```

## 📊 Interpreting Results

### Scenario A: Empty logs `[]`
**Problem:** Vapi is not sending webhooks to your server

**Check:**
1. Vapi Dashboard → Your Assistant → Tools section
2. Look for `submitInterviewSlot` tool
3. Check "Server URL" field - should be: `https://portal.risebird.io/VapiInterviewCall/Webhook`
4. Make sure it's saved

**Fix:**
- Update the webhook URL in Vapi dashboard
- Make another test call

### Scenario B: Logs show `"processingStatus": "error"`
**Problem:** Webhook received but crashed during processing

**Action:**
1. Look at `errorMessage` field in the response
2. Common errors:
   - "Table not found" → Run the CREATE TABLE script (Step 1)
   - "Connection timeout" → Check database connection string
   - "Invalid column" → Run database migration scripts

### Scenario C: Logs show `"processingStatus": "skipped"`
**Problem:** Data validation failed

**Action:**
1. Look at `errorMessage` field
2. Common reasons:
   - "Failed to parse date/time" → Assistant not collecting proper format
   - "Candidate not confirmed" → Assistant not getting explicit "yes"

**Fix:**
- Check assistant instructions
- Make sure date format is YYYY-MM-DD
- Make sure time format is HH:MM AM/PM

### Scenario D: Logs show `"processingStatus": "success"` and `"dataSaved": true`
**Problem:** Actually working! But maybe not seeing data?

**Action:**
Check VapiCallLogs table:
```sql
SELECT * FROM VapiCallLogs 
ORDER BY CreatedAt DESC;
```

## 🔍 Quick Database Checks

### Check if any data exists in VapiCallLogs:
```sql
SELECT COUNT(*) as TotalRecords FROM VapiCallLogs;
```

### Check recent webhook attempts:
```sql
SELECT TOP 10 
    RequestTimestamp,
    WebhookType,
    ProcessingStatus,
    DataSaved,
    ErrorMessage
FROM VapiWebhookDebugLogs
ORDER BY RequestTimestamp DESC;
```

### Check for specific call:
```sql
-- Get CallId from webhook debug logs first, then:
SELECT * FROM VapiCallLogs WHERE VapiCallId = 'paste-call-id-here';
```

## 🎯 Most Likely Issues

Based on your symptom (no data in VapiCallLogs), the most likely causes are:

### 1. Vapi Not Sending Webhooks (60% probability)
- Tool webhook URL not configured
- Tool not added to assistant
- Function not being called by assistant

### 2. Database Table Missing (20% probability)
- VapiCallLogs table exists but VapiWebhookDebugLogs doesn't
- Application can't write to database

### 3. Data Validation Failing (15% probability)
- Date/time in wrong format
- Candidate not confirming

### 4. Silent Error (5% probability)
- Exception being swallowed somewhere
- Network timeout

## 📞 Next Steps After This

Once you complete these steps, reply with:
1. Result of WebhookTest (Step 4)
2. Result of WebhookDebugLogs (Step 6)
3. Any error messages you see

Then I can pinpoint the exact issue and provide a targeted fix.
