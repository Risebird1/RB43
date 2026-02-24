# Vapi Timer Function - Quick Start Guide

## 🚀 What You Need to Know

A new Azure Timer Function automatically calls candidates every 15 minutes (9 AM - 9 PM) to schedule interviews using Vapi AI.

## ✅ What's Been Done

1. ✅ Created `VapiInterviewSchedulingJob.cs` timer function
2. ✅ Created `VapiEligibleCandidateVM.cs` data model
3. ✅ Added repository method to fetch eligible candidates
4. ✅ Registered services in dependency injection
5. ✅ Implemented smart eligibility logic (max 2 attempts, no duplicate bookings)

## 📋 Pre-Deployment Checklist

### ⚠️ CRITICAL: Database Migration Required

**Before deploying the code, you MUST run the database migration:**

1. Run `Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql` to add IRFId column
2. Run `Risebird.DB/StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql` to create/update stored procedure

**Why**: The code expects IRFId column to exist. Without it, calls will fail.

### Required Configuration

Ensure these are set in your Azure Function Configuration:

```
HostDetails:baseUrl = https://your-production-domain.com
Vapi:ApiKey = sk-your-vapi-api-key
Vapi:BaseUrl = https://api.vapi.ai
Vapi:PhoneNumberId = your-vapi-phone-number-id
Vapi:InterviewBookingAssistantId = asst-your-assistant-id
ConnectionString:DBConnectionString = your-sql-connection-string
```

### Verify Database Schema

**IMPORTANT**: Run the database migration first!

```sql
-- Step 1: Add IRFId column to VapiCallLogs (if not exists)
-- Run this script: Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql
```

Then verify all tables are ready:

```sql
-- Check if all required tables exist
SELECT 
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Candidate') THEN 'OK' ELSE 'MISSING' END AS Candidate_Table,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IRF') THEN 'OK' ELSE 'MISSING' END AS IRF_Table,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InterviewMeetings') THEN 'OK' ELSE 'MISSING' END AS InterviewMeetings_Table,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VapiCallLogs') THEN 'OK' ELSE 'MISSING' END AS VapiCallLogs_Table,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CandidatesMaster') THEN 'OK' ELSE 'MISSING' END AS CandidatesMaster_Table,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserList') THEN 'OK' ELSE 'MISSING' END AS UserList_Table;

-- Check if IRFId column exists in VapiCallLogs
SELECT 
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE Name = 'IRFId' AND Object_ID = Object_ID('VapiCallLogs')
    ) THEN 'OK' ELSE 'MISSING - RUN MIGRATION!' END AS IRFId_Column;
```

All should show 'OK'.

## 🎯 How It Works

### Every 15 Minutes (Between 9 AM - 9 PM)

1. **Fetch 10 eligible candidates** who:
   - Don't have confirmed interview (`InterviewMeetings.comfirmationStatus != 1`)
   - Haven't been called 2 times already
   - Have valid email and phone

2. **For each candidate**:
   - Format phone number (+91 prefix)
   - Generate dynamic prompt with today's dates
   - Initiate Vapi call
   - Save call log
   - Wait 2 seconds (rate limiting)

3. **During the call** (handled by Vapi):
   - AI assistant greets candidate
   - Explains urgent 2-day requirement
   - Books interview slot
   - Confirms details
   - Sends webhook to your server

4. **After successful booking**:
   - Webhook receives data
   - `VapiCallLogs.InterviewBooked` set to true
   - Candidate won't be called again

## 📊 Monitoring

### Check if Function is Running

```sql
-- See recent timer function calls
SELECT TOP 20
    RecipientName,
    RecipientEmail,
    RecipientPhone,
    CallStatus,
    InterviewBooked,
    CreatedAt,
    CreatedBy
FROM VapiCallLogs
WHERE CreatedBy = 'VapiTimerFunction'
ORDER BY CreatedAt DESC;
```

### Check Eligible Candidates

```sql
-- See who will be called next
WITH CandidateCallCounts AS (
    SELECT 
        RecipientEmail,
        COUNT(*) AS CallAttempts
    FROM VapiCallLogs
    WHERE RecipientEmail IS NOT NULL
    GROUP BY RecipientEmail
)
SELECT TOP 10
    c.Name,
    c.EmailId,
    c.Phone,
    irf.Name AS JobTitle,
    ISNULL(ccc.CallAttempts, 0) AS PreviousAttempts
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
LEFT JOIN CandidateCallCounts ccc ON c.EmailId = ccc.RecipientEmail
WHERE 
    c.EmailId IS NOT NULL
    AND c.Phone IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM InterviewMeetings im
        WHERE im.candidateEmail = c.EmailId
        AND CAST(im.IRFId AS INT) = c.IRFId
        AND CAST(im.comfirmationStatus AS INT) = 1
    )
    AND ISNULL(ccc.CallAttempts, 0) < 2
ORDER BY ISNULL(ccc.CallAttempts, 0) ASC, c.id DESC;
```

### Check Success Rate

```sql
-- Today's performance
SELECT 
    COUNT(*) AS TotalCalls,
    SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) AS SuccessfulBookings,
    (SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) AS SuccessRate,
    AVG(CASE WHEN CallEndedAt IS NOT NULL AND CallStartedAt IS NOT NULL 
        THEN DATEDIFF(SECOND, CallStartedAt, CallEndedAt) 
        ELSE NULL END) AS AvgCallDurationSeconds
FROM VapiCallLogs
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
AND CreatedBy = 'VapiTimerFunction';
```

## 🐛 Troubleshooting

### Problem: Function Not Running

**Check 1**: Current time
```
Is it between 9 AM - 9 PM? The function only runs during business hours.
```

**Check 2**: Azure Function logs
```
Look for: "Vapi Interview Scheduling Job triggered at: [time]"
```

**Check 3**: Configuration
```
Verify all Vapi:* settings are configured
```

### Problem: No Candidates Being Called

**Check 1**: Are there eligible candidates?
```sql
-- Run the eligibility check query above
-- Should return at least 1 row
```

**Check 2**: Do candidates have phone numbers?
```sql
SELECT COUNT(*) FROM Candidate WHERE Phone IS NULL OR Phone = '';
```

**Check 3**: Check InterviewMeetings
```sql
-- See if candidates already have confirmed meetings
SELECT * FROM InterviewMeetings 
WHERE CAST(comfirmationStatus AS INT) = 1;
```

### Problem: Calls Failing

**Check 1**: Vapi API Key
```
Verify the API key is valid and not expired
```

**Check 2**: Phone Number Format
```
Must be in E.164 format: +919876543210
```

**Check 3**: Assistant ID
```
Verify the assistant exists in Vapi dashboard
```

### Problem: Candidates Called More Than Twice

**Check**: VapiCallLogs records
```sql
-- Find candidates with > 2 attempts
SELECT 
    RecipientEmail,
    COUNT(*) AS Attempts
FROM VapiCallLogs
GROUP BY RecipientEmail
HAVING COUNT(*) > 2;
```

If this shows candidates with >2 attempts, there might be a race condition. Add logging to identify the issue.

## 📈 Expected Performance

### First Week
- **Eligible Candidates**: 50-200 (depends on your data)
- **Calls Made**: 50-480 per day
- **Success Rate**: 15-30% (learning period)
- **Avg Call Duration**: 2-4 minutes

### After Optimization
- **Success Rate**: 30-50%
- **Avg Call Duration**: 1.5-3 minutes
- **Booking Rate**: 5-20 interviews per day

## 🔧 Common Adjustments

### Change Operating Hours

Edit the time check in `VapiInterviewSchedulingJob.cs`:

```csharp
// Current: 9 AM - 9 PM
if (currentHour < 9 || currentHour >= 21)

// Example: 8 AM - 10 PM
if (currentHour < 8 || currentHour >= 22)
```

### Change Frequency

Edit the CRON expression:

```csharp
// Current: Every 15 minutes
[TimerTrigger("0 */15 * * * *")]

// Every 30 minutes
[TimerTrigger("0 */30 * * * *")]

// Every hour
[TimerTrigger("0 0 * * * *")]
```

### Change Batch Size

Edit the count parameter:

```csharp
// Current: 10 candidates per run
var eligibleCandidates = await _vapiCallRepository.GetEligibleCandidatesForVapiCallsAsync(10);

// Change to 20
var eligibleCandidates = await _vapiCallRepository.GetEligibleCandidatesForVapiCallsAsync(20);
```

### Change Rate Limiting

Edit the delay:

```csharp
// Current: 2 second delay
await Task.Delay(2000);

// Change to 5 seconds
await Task.Delay(5000);
```

## 🎬 Quick Deployment Steps

1. **Build the solution**
   ```bash
   dotnet build Risebird.AzureTimerFunctions/Risebird.AzureTimerFunctions.csproj
   ```

2. **Publish to Azure**
   ```bash
   # Via Azure CLI
   func azure functionapp publish <your-function-app-name>
   ```

3. **Verify Configuration**
   - Go to Azure Portal → Your Function App → Configuration
   - Verify all required settings are present

4. **Enable Function**
   - Azure Portal → Your Function App → Functions
   - Find `VapiInterviewSchedulingJob`
   - Ensure it's enabled

5. **Monitor First Run**
   - Azure Portal → Your Function App → Log Stream
   - Wait for next 15-minute trigger
   - Watch logs for execution

## 📞 First Call Test

After deployment, to verify it works:

1. **Create a test candidate**:
```sql
INSERT INTO Candidate (IRFId, Name, EmailId, Phone, AddedByUserId)
VALUES (1, 'Test Candidate', 'test@example.com', '9876543210', 1);
```

2. **Wait for next 15-minute trigger** (or trigger manually in Azure Portal)

3. **Check VapiCallLogs**:
```sql
SELECT * FROM VapiCallLogs 
WHERE RecipientEmail = 'test@example.com'
ORDER BY CreatedAt DESC;
```

4. **Check Vapi Dashboard** for call recording and status

## 🎉 You're All Set!

The timer function is now ready to automatically schedule interviews for your candidates. It will:

✅ Run every 15 minutes during business hours  
✅ Call up to 10 eligible candidates per run  
✅ Track all attempts in VapiCallLogs  
✅ Respect the 2-attempt limit  
✅ Use GPT 5.2 Instant for optimal performance  
✅ Emphasize 2-day urgency politely  

Monitor the logs and adjust as needed based on real-world performance!
