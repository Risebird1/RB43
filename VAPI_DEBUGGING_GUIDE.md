# Vapi Webhook Debugging System

## Overview
A comprehensive debugging system has been added to track every webhook request from Vapi and identify why data isn't being saved.

## 🔧 Setup Steps

### Step 1: Create the Debug Table
Run this SQL script on your database:

```sql
-- Table to debug and monitor all Vapi webhook requests
CREATE TABLE [dbo].[VapiWebhookDebugLogs]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestTimestamp] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [WebhookType] NVARCHAR(100) NULL,
    [CallId] NVARCHAR(100) NULL,
    [AssistantId] NVARCHAR(100) NULL,
    [ToolCallName] NVARCHAR(100) NULL,
    [RawPayload] NVARCHAR(MAX) NULL,
    [ProcessingStatus] NVARCHAR(50) NULL, -- success, error, skipped
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CallLogFound] BIT NULL,
    [CallLogId] INT NULL,
    [DataSaved] BIT NULL,
    
    INDEX [IX_VapiWebhookDebugLogs_CallId] NONCLUSTERED ([CallId]),
    INDEX [IX_VapiWebhookDebugLogs_RequestTimestamp] NONCLUSTERED ([RequestTimestamp] DESC),
    INDEX [IX_VapiWebhookDebugLogs_ToolCallName] NONCLUSTERED ([ToolCallName])
);
```

### Step 2: Rebuild and Restart
```bash
dotnet build
# Stop your application
# Start your application
```

## 📊 How to Use the Debugging System

### 1. Test Webhook Connectivity
Before making any calls, test if the webhook is accessible:

**URL:** `GET https://portal.risebird.io/VapiInterviewCall/WebhookTest`

**Expected Response:**
```json
{
  "status": "ok",
  "message": "Vapi webhook endpoint is accessible",
  "timestamp": "2026-01-29T...",
  "url": "https://portal.risebird.io/VapiInterviewCall/Webhook"
}
```

If this doesn't work, your webhook URL is not accessible to Vapi.

### 2. Make a Test Call
Use Vapi dashboard or your application to make a test call.

### 3. Check Debug Logs
After the call ends, check the debug logs:

**URL:** `GET https://portal.risebird.io/VapiInterviewCall/WebhookDebugLogs?count=20`

**Response Example:**
```json
{
  "success": true,
  "logs": [
    {
      "id": 1,
      "requestTimestamp": "2026-01-29T10:30:45",
      "webhookType": "tool-calls",
      "callId": "abc123",
      "toolCallName": "submitInterviewSlot",
      "rawPayload": "{...full webhook payload...}",
      "processingStatus": "success",
      "errorMessage": null,
      "callLogFound": true,
      "callLogId": 42,
      "dataSaved": true
    }
  ]
}
```

### 4. Interpret the Results

#### ✅ Success Case
```json
{
  "processingStatus": "success",
  "callLogFound": true,
  "callLogId": 42,
  "dataSaved": true
}
```
**Meaning:** Everything worked! Data was saved to VapiCallLogs table (record ID: 42)

#### ❌ No Webhooks Received
If `VapiWebhookDebugLogs` table is empty after a call:

**Problem:** Vapi is not sending webhooks to your server
**Solutions:**
- Verify webhook URL in Vapi dashboard: `https://portal.risebird.io/VapiInterviewCall/Webhook`
- Check if URL is publicly accessible (test with WebhookTest endpoint)
- Check firewall/security settings
- Check application logs for any errors

#### ❌ Error Status
```json
{
  "processingStatus": "error",
  "errorMessage": "Database save failed: ..."
}
```
**Problem:** Webhook received but processing failed
**Solutions:**
- Check the `errorMessage` field for details
- Check application logs for full stack trace
- Verify database connection
- Check if VapiCallLogs table exists

#### ⚠️ Skipped Status
```json
{
  "processingStatus": "skipped",
  "errorMessage": "Failed to parse date/time: ..."
}
```
**Problem:** Data validation failed
**Solutions:**
- Check if assistant is collecting date/time in correct format (YYYY-MM-DD, HH:MM AM/PM)
- Check if candidate explicitly confirmed
- Review `rawPayload` to see what data was actually sent

#### 🔍 Tool Call Not Found
If you see webhook logs but no "tool-calls" type:

**Problem:** Assistant is not calling the `submitInterviewSlot` function
**Solutions:**
- Verify the tool is configured in Vapi dashboard
- Check assistant instructions
- Review call transcript to see if confirmation was actually collected
- Make sure assistant has the function definition

## 🔍 Direct Database Queries

### Check Recent Webhooks
```sql
SELECT TOP 20 
    RequestTimestamp,
    WebhookType,
    CallId,
    ProcessingStatus,
    CallLogFound,
    CallLogId,
    DataSaved,
    ErrorMessage
FROM VapiWebhookDebugLogs
ORDER BY RequestTimestamp DESC;
```

### Check Webhooks for Specific Call
```sql
SELECT * FROM VapiWebhookDebugLogs 
WHERE CallId = 'your-call-id-here'
ORDER BY RequestTimestamp DESC;
```

### Check Failed Webhooks
```sql
SELECT * FROM VapiWebhookDebugLogs 
WHERE ProcessingStatus = 'error'
ORDER BY RequestTimestamp DESC;
```

### View Full Payload for Investigation
```sql
SELECT 
    CallId,
    RawPayload
FROM VapiWebhookDebugLogs 
WHERE CallId = 'your-call-id-here'
AND WebhookType = 'tool-calls';
```

## 📋 Troubleshooting Checklist

When no data appears in VapiCallLogs:

1. ✓ **Check VapiWebhookDebugLogs table**
   - If empty → Webhooks not reaching server
   - If has records → Continue to #2

2. ✓ **Check ProcessingStatus**
   - "received" → Webhook arrived
   - "tool-call-received" → Tool call detected
   - "success" → Data should be in VapiCallLogs
   - "error" → Check ErrorMessage
   - "skipped" → Check ErrorMessage for validation issue

3. ✓ **Check DataSaved field**
   - `true` → Check VapiCallLogs table with CallLogId
   - `false` or `null` → Data wasn't saved, check ErrorMessage

4. ✓ **Check Application Logs**
   - Look for "✅ SUCCESS" messages
   - Look for "❌ FAILED" or "❌ SKIPPED" messages

5. ✓ **Check VapiCallLogs directly**
   ```sql
   SELECT * FROM VapiCallLogs 
   WHERE VapiCallId = 'your-call-id'
   ```

## 🎯 Common Issues and Solutions

### Issue 1: No webhooks in debug table
**Cause:** Vapi not sending webhooks
**Fix:** 
- Verify webhook URL in Vapi dashboard
- Test with WebhookTest endpoint
- Check server accessibility

### Issue 2: Webhooks received but ProcessingStatus = "error"
**Cause:** Database or code error
**Fix:** Check ErrorMessage and application logs

### Issue 3: ProcessingStatus = "skipped"
**Cause:** Data validation failed
**Fix:** Check ErrorMessage - likely date/time format or confirmation issue

### Issue 4: ProcessingStatus = "success" but no data in VapiCallLogs
**Cause:** Database transaction issue
**Fix:** 
- Check CallLogId in debug log
- Query VapiCallLogs with that ID
- Check database transaction logs

## 💡 Enhanced Logging

The system now logs with emojis for easy identification:

- `✅ SUCCESS` - Data saved successfully
- `❌ FAILED` - Error occurred
- `❌ SKIPPED` - Validation failed
- `⚠️ WARNING` - Potential issue

Check your application error logs for these markers.

## 🧪 Test Sequence

1. Run WebhookTest endpoint
2. Make a test call from Vapi
3. Let the call complete with a confirmed interview slot
4. Check WebhookDebugLogs endpoint
5. Check VapiCallLogs table
6. Check application error logs

If any step fails, the debug logs will show exactly where and why.
