# Vapi Automated Interview Scheduling Timer Function

## Overview

The `VapiInterviewSchedulingJob` is an Azure Timer Function that automatically calls candidates using Vapi's AI voice assistant to schedule interviews.

## Execution Schedule

- **Frequency**: Every 15 minutes
- **Operating Hours**: 9 AM to 9 PM only (IST)
- **CRON Expression**: `"0 */15 * * * *"`

## Business Logic

### Candidate Selection Criteria

The function fetches candidates who meet ALL of the following conditions:

1. **No Confirmed Interview**: 
   - No entry in `InterviewMeetings` table with `comfirmationStatus = 1` for the candidate's IRFId and email

2. **Maximum 2 Call Attempts Per IRF**:
   - Candidate has been called 0 or 1 times for THIS specific IRF (tracked in `VapiCallLogs` per email+IRFId)
   - OR has been called but booking was not successful for THIS IRF (`InterviewBooked != 1`)
   - Same candidate can be called up to 2 times for each different IRF independently

3. **Valid Contact Information**:
   - Has a valid email address (from `Candidate.EmailId` or fallback to `CandidatesMaster.EmailId` or `UserList.EmailId`)
   - Has a valid phone number (from `Candidate.Phone`)

4. **Latest 10 Records**:
   - Prioritizes candidates with fewer call attempts
   - Among same attempt counts, newest candidates first

### Call Flow

For each eligible candidate:

1. **Validate phone number** (E.164 format with +91 prefix)
2. **Generate dynamic system prompt** with:
   - Current date
   - Tomorrow and day after tomorrow dates
   - IRF ID
   - Candidate email
   - Job title
3. **Initiate Vapi call** with:
   - Model: `gpt-5.2-instant`
   - Custom system prompt
   - Variable values (irfId, candidateEmail)
4. **Log call attempt** to `VapiCallLogs`
5. **Wait 2 seconds** before next call (rate limiting)

### Database Joins

The function performs the following joins to get complete candidate information:

```sql
Candidate (primary)
├── INNER JOIN IRF (to get job title)
├── LEFT JOIN CandidatesMaster (to get alternate email)
└── LEFT JOIN UserList (to get user email if needed)
```

## Files Modified/Created

### New Files

1. **VapiInterviewSchedulingJob.cs**
   - Main timer function
   - Path: `Risebird.AzureTimerFunctions/VapiInterviewSchedulingJob.cs`

2. **VapiEligibleCandidateVM.cs**
   - View model for eligible candidates
   - Path: `Risebird.Entities/ViewModel/VapiEligibleCandidateVM.cs`

### Modified Files

1. **IVapiCallRepository.cs**
   - Added: `GetEligibleCandidatesForVapiCallsAsync(int count = 10)`
   
2. **VapiCallRepository.cs**
   - Implemented: `GetEligibleCandidatesForVapiCallsAsync()` calling stored procedure
   - Updated: `SaveCallLogAsync` to include IRFId
   - Updated: `UpdateCallLogAsync` to include IRFId

3. **VapiCallLog.cs**
   - Added: `IRFId` property for per-IRF call tracking

4. **VapiCallLogs.sql**
   - Added: `IRFId INT NULL` column
   - Added: Performance indexes

5. **VapiInterviewCallController.cs**
   - Updated: Call log creation to save IRFId

6. **IServiceCollectionExtensions.cs**
   - Added: `IVapiService` and `IVapiCallRepository` registrations
   - Added: `AddHttpClient()` for HTTP calls

7. **usp_GetEligibleCandidatesForVapiCalls.sql** (Stored Procedure)
   - Created stored procedure with IRFId tracking logic
   - Groups call attempts by email AND IRFId

8. **Add_IRFId_To_VapiCallLogs.sql** (Migration)
   - Migration script to add IRFId column safely

## Configuration Required

Ensure the following configuration values are set in `appsettings.json` or Azure App Settings:

```json
{
  "HostDetails": {
    "baseUrl": "https://your-app-url.com"
  },
  "Vapi": {
    "ApiKey": "your-vapi-api-key",
    "BaseUrl": "https://api.vapi.ai",
    "PhoneNumberId": "your-phone-number-id",
    "InterviewBookingAssistantId": "your-assistant-id"
  },
  "ConnectionString": {
    "DBConnectionString": "your-database-connection-string"
  }
}
```

## Monitoring & Logging

### Logs to Monitor

1. **Function Execution Logs**:
   - Trigger time
   - Business hours check
   - Number of eligible candidates found
   - Success/failure counts

2. **VapiCallLogs Table**:
   - Tracks all call attempts
   - `CreatedBy = 'VapiTimerFunction'` for timer-initiated calls
   - `InterviewBooked` field indicates success

3. **VapiWebhookCheckpoints Table**:
   - Detailed execution checkpoints
   - Tool call processing
   - Interview slot submissions

### Key Metrics to Track

- **Call Success Rate**: `(Successful bookings / Total calls) * 100`
- **Average Attempts per Booking**: How many calls needed to book an interview
- **Peak Call Times**: When candidates are most responsive
- **Failure Reasons**: Track common objections or technical issues

## Testing

### Local Testing

1. Set `appsettings.json` with valid configuration
2. Run the function locally: The timer will execute immediately and then every 15 minutes
3. Monitor console logs for execution details

### Manual Testing

To test a specific candidate without waiting for the timer:

```csharp
// Call the controller endpoint directly
POST /VapiInterviewCall/InitiateInterviewCall
{
    "interviewId": 0,
    "candidateId": 123,
    "candidateName": "John Doe",
    "candidateEmail": "john@example.com",
    "candidatePhone": "+919876543210",
    "jobTitle": "Software Engineer",
    "irfId": "456"
}
```

## Business Rules Summary

| Rule | Description |
|------|-------------|
| **Max Calls** | 2 attempts per candidate |
| **Operating Hours** | 9 AM - 9 PM only |
| **Frequency** | Every 15 minutes |
| **Batch Size** | 10 candidates per run |
| **Interview Window** | Next 2 days only |
| **Rate Limiting** | 2 second delay between calls |

## Troubleshooting

### Function Not Running

1. Check if time is within 9 AM - 9 PM
2. Verify timer trigger CRON expression
3. Check Azure Function logs for errors

### No Candidates Being Called

1. Verify candidates meet eligibility criteria
2. Check if candidates already have confirmed interviews
3. Verify candidate phone numbers are valid
4. Check if call attempt limit (2) has been reached

### Calls Failing

1. Verify Vapi API key is valid
2. Check phone number format (must be E.164)
3. Verify assistant ID exists in Vapi
4. Check webhook URL is accessible from Vapi

### Calls Not Reaching Candidates

1. Verify phone numbers are correct and active
2. Check Vapi dashboard for call status
3. Review call logs in `VapiCallLogs` table
4. Check if phone numbers are in DND list

## SQL Queries for Monitoring

### Get Call Statistics

```sql
-- Get call success rate by date
SELECT 
    CAST(CreatedAt AS DATE) AS CallDate,
    COUNT(*) AS TotalCalls,
    SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) AS SuccessfulBookings,
    (SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) AS SuccessRate
FROM VapiCallLogs
WHERE CreatedBy = 'VapiTimerFunction'
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY CallDate DESC;
```

### Get Candidates with Multiple Attempts

```sql
-- Find candidates called multiple times
SELECT 
    RecipientEmail,
    RecipientName,
    COUNT(*) AS CallAttempts,
    MAX(CASE WHEN InterviewBooked = 1 THEN 'Yes' ELSE 'No' END) AS EverBooked,
    MAX(CreatedAt) AS LastCallDate
FROM VapiCallLogs
WHERE CreatedBy = 'VapiTimerFunction'
GROUP BY RecipientEmail, RecipientName
HAVING COUNT(*) > 1
ORDER BY CallAttempts DESC;
```

### Get Eligible Candidates Count

```sql
-- Check how many candidates are eligible for next run
WITH CandidateCallCounts AS (
    SELECT 
        RecipientEmail,
        COUNT(*) AS CallAttempts
    FROM VapiCallLogs
    WHERE RecipientEmail IS NOT NULL
    GROUP BY RecipientEmail
)
SELECT COUNT(*) AS EligibleCandidatesCount
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
    AND ISNULL(ccc.CallAttempts, 0) < 2;
```

## Integration Points

### Incoming

- **Database Tables**: Candidate, IRF, InterviewMeetings, CandidatesMaster, UserList
- **Configuration**: Vapi API keys, phone numbers, webhook URLs

### Outgoing

- **Vapi API**: Initiates phone calls
- **VapiCallLogs**: Records all call attempts
- **Webhook**: Receives booking confirmations at `/VapiInterviewCall/Webhook`

## Future Enhancements

1. **Smart Scheduling**: Call candidates at optimal times based on historical success rates
2. **Priority Queue**: Prioritize urgent IRFs or high-value candidates
3. **Retry Logic**: Exponential backoff for failed calls
4. **A/B Testing**: Test different system prompts or voice settings
5. **Analytics Dashboard**: Real-time monitoring of call metrics
6. **Multi-language Support**: Support for regional languages

## Support

For issues or questions:
1. Check Azure Function logs
2. Review `VapiCallLogs` and `VapiWebhookCheckpoints` tables
3. Verify Vapi dashboard for call recordings
4. Contact Vapi support for API issues
