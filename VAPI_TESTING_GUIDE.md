# Vapi Automated Calls - Testing Guide

## 🎯 Quick Test via Dashboard

I've added the timer function logic to your DashboardController so you can test it immediately without waiting for the timer to trigger.

---

## 🚀 How to Test

### Step 1: Run the Project

```bash
cd Risebird.Web
dotnet run
```

### Step 2: Access the Test URL

Navigate to:

```
https://localhost:5001/Dashboard/Index?testVapiCalls=true
```

Or if using HTTP:

```
http://localhost:5000/Dashboard/Index?testVapiCalls=true
```

**Important**: You must be logged in as Admin, HR, SeniorHR, SupportAdmin, Quality Controller, InterviewerManagement, SeniorInterviewerManagement, or TechSupport.

---

## 📋 What Happens When You Test

The page will:

1. **Check Business Hours** (9 AM - 9 PM IST)
   - If outside hours: Shows message and skips execution
   - If within hours: Proceeds to next step

2. **Fetch Eligible Candidates** (via stored procedure)
   - Calls `usp_GetEligibleCandidatesForVapiCalls`
   - Gets up to 10 eligible candidates
   - Shows how many found

3. **Verify Configuration**
   - Checks for Vapi Assistant ID
   - Validates webhook URL
   - Shows configuration details

4. **Process Each Candidate**
   - Validates phone number
   - Formats phone (adds +91 if needed)
   - Creates system prompt with current date
   - Initiates Vapi call
   - Saves call log with IRFId
   - Shows success/failure for each

5. **Display Summary**
   - Total candidates processed
   - Successful calls
   - Failed calls

---

## 📊 Test Results Page

The test results are displayed in a dark-themed, formatted page showing:

- ✅ Green text for success
- ❌ Red text for errors
- ⚠️ Yellow text for warnings/skipped
- 📋 Blue text for informational messages

**Example output**:

```
🕐 Test initiated at: 2026-02-24 14:30:00
✅ Within business hours - Current time: 14:30
📋 Fetching eligible candidates...
✅ Found 3 eligible candidates
✅ Using Assistant ID: 5a7c4ecb-fcba-4fa6-bcf1-48842d742144
✅ Webhook URL: https://yourapp.com/VapiInterviewCall/Webhook

📞 Processing: John Doe
   Email: john@example.com
   Phone: 9876543210
   IRF ID: 1 (IRF-2026-001)
   Previous Attempts: 0
   ✅ Call initiated - CallId: abc123, Status: queued
   ✅ Call log saved to database

📞 Processing: Jane Smith
   Email: jane@example.com
   Phone: 9876543211
   IRF ID: 2 (IRF-2026-002)
   Previous Attempts: 1
   ✅ Call initiated - CallId: def456, Status: queued
   ✅ Call log saved to database

═══════════════════════════════════
📊 SUMMARY:
   Total Candidates: 3
   Successful Calls: 2
   Failed Calls: 1
═══════════════════════════════════
```

---

## 🔍 What to Verify

### 1. Stored Procedure Works
- Check if candidates are returned
- Verify email comes from UserList (via CandidatesMaster)
- Confirm IRFId is correct

### 2. Three-Way Join Works
- Candidate → CandidatesMaster → UserList
- Email matches UserList.EmailId
- No NULL emails

### 3. Call Tracking per IRF Works
- Same email can appear for different IRFs
- Call attempts tracked separately per IRF
- Max 2 calls per (email + IRF) combination

### 4. Vapi Integration Works
- Calls initiated successfully
- Call logs saved with IRFId
- IRFId and candidateEmail passed as variables

### 5. System Prompt Correct
- Contains current dates
- Has 2-day urgency messaging
- Includes irfId and candidateEmail values

---

## 🛠️ Configuration Required

Make sure these are set in `appsettings.json` or environment variables:

```json
{
  "Vapi": {
    "ApiKey": "your-vapi-api-key",
    "PhoneNumberId": "your-phone-number-id",
    "InterviewBookingAssistantId": "your-assistant-id"
  },
  "HostDetails": {
    "baseUrl": "https://yourapp.com"
  }
}
```

---

## 🧪 Test Scenarios

### Test 1: Fresh Candidate (0 Attempts)
- Should make call
- Should save with CallAttempts = 0
- IRFId should be saved

### Test 2: Candidate with 1 Attempt (No Booking)
- Should make 2nd call (eligible for retry)
- CallAttempts = 1 in results
- Same email, different IRF → should also be called

### Test 3: Candidate with 2 Attempts
- Should NOT appear in results (maxed out for that IRF)

### Test 4: Candidate with Confirmed Interview
- Should NOT appear in results (already has interview)

### Test 5: Outside Business Hours
- Access URL at 8 AM or 10 PM
- Should show "outside business hours" message
- No calls made

---

## 🎯 Normal Dashboard Access

To access the normal dashboard (without testing):

```
https://localhost:5001/Dashboard/Index
```

(Without the `testVapiCalls=true` parameter)

---

## 🐛 Debugging

### If No Candidates Found

Check the stored procedure:

```sql
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 10;
```

**Expected**: Returns candidates with:
- Valid email from UserList
- Valid phone from Candidate
- No confirmed interview
- < 2 call attempts for that IRF

### If Configuration Error

Check `appsettings.json`:
- Vapi:InterviewBookingAssistantId must be set
- Vapi:ApiKey must be valid
- Vapi:PhoneNumberId must be valid

### If Calls Fail

Check:
- Vapi API key is valid
- Phone numbers are in correct format (+91XXXXXXXXXX)
- Assistant ID exists in your Vapi dashboard
- Network connectivity

---

## 📱 Verify Calls in Vapi Dashboard

After running the test:

1. Log into your Vapi.ai dashboard
2. Navigate to "Calls" section
3. You should see the initiated calls
4. Status will be one of:
   - `queued` - Waiting to be placed
   - `ringing` - Currently ringing
   - `in-progress` - Call answered
   - `ended` - Call completed

---

## 💾 Verify Database Logs

Check VapiCallLogs table:

```sql
SELECT TOP 10
    VapiCallId,
    IRFId,
    RecipientName,
    RecipientEmail,
    RecipientPhone,
    CallStatus,
    CreatedBy,
    CreatedAt
FROM VapiCallLogs
ORDER BY CreatedAt DESC;
```

**Look for**:
- IRFId populated (not NULL)
- RecipientEmail matches UserList.EmailId
- CreatedBy = "Test-{your-email}"
- Recent CreatedAt timestamp

---

## 🔄 Running Multiple Tests

You can run the test multiple times:
- Click "🔄 Run Test Again" button on results page
- Or navigate to URL again: `/Dashboard/Index?testVapiCalls=true`

**Note**: Eligible candidates will decrease after each test since call attempts are tracked!

---

## ⚡ Quick Verification Commands

### See Eligible Candidates Count

```sql
-- How many candidates are eligible right now?
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 100;
```

### See Call Attempts per IRF

```sql
-- Check call attempts grouped by email and IRF
SELECT 
    RecipientEmail,
    IRFId,
    COUNT(*) AS CallAttempts,
    MAX(CreatedAt) AS LastCallTime
FROM VapiCallLogs
WHERE RecipientEmail IS NOT NULL
GROUP BY RecipientEmail, IRFId
ORDER BY IRFId, CallAttempts DESC;
```

### Reset Test Data (If Needed)

```sql
-- Delete test call logs to reset
DELETE FROM VapiCallLogs
WHERE CreatedBy LIKE 'Test-%';

-- Now eligible candidates will be back to 0 attempts
```

---

## ✅ Success Indicators

After testing, you should see:

1. **Stored Procedure Returns Data**
   - Candidates with valid emails
   - Emails from UserList (via CandidatesMaster)
   - IRFId populated

2. **Vapi Calls Initiated**
   - CallId returned
   - Status = "queued" initially

3. **Database Logs Created**
   - VapiCallLogs entries
   - IRFId populated
   - RecipientEmail matches

4. **Three-Way Join Works**
   - Email comes from UserList
   - Via Candidate → CandidatesMaster → UserList chain

5. **Per-IRF Tracking Works**
   - Same email can be called for different IRFs
   - Call attempts tracked separately per IRF

---

## 🎊 Ready for Production

Once testing is successful:

1. Remove the test code from DashboardController (optional - it's gated by query parameter)
2. Deploy the timer function: `VapiInterviewSchedulingJob.cs`
3. Timer will run automatically every 15 minutes during business hours

---

**Testing URL**: `/Dashboard/Index?testVapiCalls=true`  
**Normal Dashboard**: `/Dashboard/Index`  
**Date**: February 24, 2026  
**Status**: ✅ Ready for Testing!

---

## 📝 Model Information

**Current Model**: `gpt-5-mini`

This model provides:
- ✅ Fast response times (optimized for speed)
- ✅ Good quality conversations
- ✅ Cost-effective for high-volume calling
- ✅ Valid Vapi model identifier

**Note**: The original `gpt-5.2-instant` was invalid and has been corrected to `gpt-5-mini`.
