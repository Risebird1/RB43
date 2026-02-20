# 🚨 CRITICAL: How To Test Vapi Integration

## The Problem You Encountered

You saw `interviewDate: "2023-10-30"` (old date) and no data in `VapiCallLogs` table.

**Why?**
You initiated the call from **Vapi Dashboard** instead of your **Application API**.

---

## ❌ WRONG WAY (Don't Do This)

```
Vapi Dashboard → Click "Test Call" → Wrong Date → Data Issues
```

**What happens:**
- Assistant uses BASE system prompt (without dynamic date injection)
- Assistant doesn't know today's date
- Assistant hallucinates old dates like "2023-10-30"
- Data might not save properly

---

## ✅ CORRECT WAY (Do This Instead)

### Step 1: Restart Application

```bash
# Stop and restart your app to load new code
```

### Step 2: Call Through YOUR Application API

**Option A: Using Postman/Insomnia**
```http
POST https://portal.risebird.io/VapiInterviewCall/InitiateInterviewCall
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN

{
  "interviewId": 123,
  "candidateId": 456,
  "candidateName": "Harsh Test",
  "candidateEmail": "test@risebird.io",
  "candidatePhone": "+919999999999",
  "jobTitle": "Software Engineer"
}
```

**Option B: Using cURL**
```bash
curl -X POST https://portal.risebird.io/VapiInterviewCall/InitiateInterviewCall \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "interviewId": 123,
    "candidateId": 456,
    "candidateName": "Harsh Test",
    "candidateEmail": "test@risebird.io",
    "candidatePhone": "+919999999999",
    "jobTitle": "Software Engineer"
  }'
```

**Option C: From Your Own Application UI**
Use whatever button/form in your app that normally triggers interview calls.

### Step 3: Answer The Call

1. Phone will ring at `+919999999999`
2. Answer and follow assistant prompts
3. When asked for availability, say: **"Today at 6 PM"**
4. Confirm when assistant asks for confirmation: **"Yes, that's correct"**

### Step 4: Verify Results

**Check 1: Webhook Debug Logs**
```sql
SELECT TOP 10
    Id,
    RequestTimestamp,
    CallId,
    ProcessingStatus,
    ErrorMessage,
    CallLogFound,
    DataSaved
FROM VapiWebhookDebugLogs 
ORDER BY RequestTimestamp DESC;
```

**Expected:**
- `ProcessingStatus` = "success"
- `DataSaved` = 1
- `ErrorMessage` = NULL

**Check 2: Call Logs**
```sql
SELECT TOP 10
    VapiCallId,
    RecipientName,
    RecipientPhone,
    InterviewBooked,
    BookedInterviewDateTime,
    BookingConfirmationDetails
FROM VapiCallLogs
ORDER BY CreatedAt DESC;
```

**Expected:**
- `InterviewBooked` = 1
- `BookedInterviewDateTime` = **2026-01-30** or **2026-01-31** (NOT 2023-*)
- `RecipientName` = "Harsh Test"

---

## 🎯 Success Criteria

✅ Call initiated without error  
✅ Phone rings and connects  
✅ Assistant speaks with correct date context  
✅ Date in tool call is 2026-01-30 or 2026-01-31  
✅ Data appears in `VapiCallLogs` table  
✅ Debug logs show "success" status  

---

## 🚨 Common Mistakes

### Mistake 1: Testing from Vapi Dashboard
**Problem:** Dynamic date injection doesn't work  
**Solution:** Use application API endpoint

### Mistake 2: Forgetting to Restart App
**Problem:** Old code still running  
**Solution:** Stop and restart application

### Mistake 3: Wrong Phone Number Format
**Problem:** Call fails to connect  
**Solution:** Use E.164 format with country code: `+91xxxxxxxxxx`

---

## 🔍 Troubleshooting

### Issue: Call Doesn't Initiate

**Check 1: API Response**
```json
{
  "success": false,
  "message": "..."  // ← Check this error
}
```

**Check 2: Configuration**
```json
// appsettings.json
{
  "Vapi": {
    "ApiKey": "...",  // ← Should be your PRIVATE key
    "PhoneNumberId": "...",  // ← Should be Vapi phone number ID
    "InterviewBookingAssistantId": "..."  // ← Should be your assistant ID
  }
}
```

### Issue: Call Connects But Wrong Date

**Symptom:** Assistant says dates from 2023 or other old year

**Diagnosis:**
```sql
-- Check debug logs
SELECT RawPayload 
FROM VapiWebhookDebugLogs 
WHERE CallId = 'YOUR_CALL_ID';

-- Look for "interviewDate" field in the JSON
-- If it shows 2023-*, the call was from dashboard
```

**Solution:** You called from Vapi dashboard. Use application API instead.

### Issue: Data Not Saving

**Check 1: Error in Debug Logs**
```sql
SELECT 
    CallId,
    ProcessingStatus,
    ErrorMessage
FROM VapiWebhookDebugLogs 
WHERE ProcessingStatus = 'error'
ORDER BY RequestTimestamp DESC;
```

**Check 2: Application Error Logs**
Look for messages containing:
- `❌ FAILED to save call log`
- Database constraint violations
- Null reference exceptions

---

## 📞 Support Checklist

Before reporting an issue, provide:

1. **How you initiated the call:** (Dashboard or API)
2. **API response:** (if using API)
3. **CallId:** (from webhook debug logs)
4. **Debug log entry:** (SQL query results)
5. **Error message:** (from debug logs or application logs)

---

## 🎓 Understanding Dynamic Date Injection

**When you call through application:**
```
Application → Injects current date in system prompt → Vapi → Uses correct date
```

**When you call from dashboard:**
```
Dashboard → Uses base prompt (no date) → Vapi → Guesses wrong date
```

**The system prompt injected:**
```
CURRENT DATE INFORMATION (CRITICAL):
- Today is: Thursday, February 20, 2026 (2026-02-20)
- Tomorrow is: 2026-02-21
- Day after tomorrow is: 2026-02-22
- ONLY these dates are allowed: 2026-02-21 OR 2026-02-22
```

This is WHY you must test through the application!

---

**REMEMBER: Always test through `/InitiateInterviewCall`, NEVER from Vapi dashboard!**
