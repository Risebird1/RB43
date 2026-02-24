# Vapi Timer Function Implementation Summary

## What Was Implemented

An Azure Timer Function that automatically calls candidates using Vapi AI assistant to schedule interviews.

## Key Features

✅ **Runs every 15 minutes** between 9 AM - 9 PM  
✅ **Fetches top 10 eligible candidates** per run  
✅ **Maximum 2 call attempts** per candidate  
✅ **Smart eligibility logic** - only calls candidates who need interviews  
✅ **Automatic retry** - candidates eligible for second call if first attempt didn't result in booking  
✅ **Complete logging** - all attempts tracked in VapiCallLogs  

## Files Created

### 1. **VapiInterviewSchedulingJob.cs**
Location: `Risebird.AzureTimerFunctions/VapiInterviewSchedulingJob.cs`

Main timer function with:
- Time-based execution (9 AM - 9 PM only)
- Candidate eligibility check
- Vapi call initiation
- Error handling and logging
- Rate limiting (2 second delay between calls)

### 2. **VapiEligibleCandidateVM.cs**
Location: `Risebird.Entities/ViewModel/VapiEligibleCandidateVM.cs`

View model containing:
- CandidateId, IRFId, IRFName
- CandidateName, CandidateEmail, CandidatePhone
- JobTitle
- CallAttempts, LastCallAttempt

## Files Modified

### 1. **IVapiCallRepository.cs**
Added method:
```csharp
Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForVapiCallsAsync(int count = 10);
```

### 2. **VapiCallRepository.cs**
Implemented `GetEligibleCandidatesForVapiCallsAsync()` with complex SQL:
- Joins: Candidate → IRF → CandidatesMaster → UserList
- CTE for counting call attempts
- Filters for confirmed interviews
- Limits to max 2 attempts
- Returns top 10 candidates

### 3. **IServiceCollectionExtensions.cs**
Added dependency injection:
- `AddHttpClient()` - for HTTP calls
- `IVapiService` → `VapiService`
- `IVapiCallRepository` → `VapiCallRepository`

## Eligibility Logic (SQL)

A candidate is eligible if:

```sql
✓ Has valid email (Candidate.EmailId OR CandidatesMaster.EmailId OR UserList.EmailId)
✓ Has valid phone number (Candidate.Phone)
✓ No confirmed interview in InterviewMeetings (comfirmationStatus != 1)
✓ Called less than 2 times OR no successful booking yet
```

### Priority Order:
1. Candidates with 0 attempts (never called)
2. Candidates with 1 attempt (eligible for retry)
3. Newest candidates first (by Candidate.id DESC)

## Call Attempt Tracking

### First Attempt
- Candidate has 0 entries in `VapiCallLogs`
- Gets called
- Entry created in `VapiCallLogs` with `CreatedBy = 'VapiTimerFunction'`

### Second Attempt (Retry)
- Candidate has 1 entry in `VapiCallLogs`
- AND `InterviewBooked != 1` (booking not successful)
- Gets called again in next eligible run
- Second entry created in `VapiCallLogs`

### No Third Attempt
- Candidate has 2 entries in `VapiCallLogs`
- Will NOT be called again regardless of booking status

## Data Flow

```
Timer Trigger (Every 15 minutes)
    ↓
Check Time (9 AM - 9 PM only)
    ↓
Query Database (GetEligibleCandidatesForVapiCallsAsync)
    ↓
For each candidate (max 10):
    ├─ Format phone number (E.164)
    ├─ Generate system prompt (dynamic dates)
    ├─ Create VapiCallRequest
    ├─ Initiate call via Vapi API
    ├─ Save to VapiCallLogs
    └─ Wait 2 seconds
    ↓
Log results (success/failure counts)
```

## Configuration Required

In `appsettings.json` or Azure Configuration:

```json
{
  "HostDetails": {
    "baseUrl": "https://your-production-url.com"
  },
  "Vapi": {
    "ApiKey": "sk-...",
    "BaseUrl": "https://api.vapi.ai",
    "PhoneNumberId": "your-phone-number-id",
    "InterviewBookingAssistantId": "asst_..."
  },
  "ConnectionString": {
    "DBConnectionString": "Server=...;Database=...;..."
  }
}
```

## Deployment Checklist

- [ ] Update `appsettings.json` with production values
- [ ] Verify Vapi Assistant ID is correct
- [ ] Verify webhook URL is accessible from internet
- [ ] Test with a small batch of candidates first
- [ ] Monitor logs for first few runs
- [ ] Verify VapiCallLogs entries are being created
- [ ] Check webhook receives data correctly
- [ ] Confirm InterviewMeetings updates after successful bookings

## Testing Strategy

### Phase 1: Local Testing (Development)
1. Set up local configuration
2. Create test candidates in database
3. Run function manually
4. Verify calls are initiated
5. Check VapiCallLogs for entries

### Phase 2: Small Batch Testing (Staging)
1. Deploy to staging environment
2. Let function run for 1 day
3. Monitor 10-20 real calls
4. Analyze success rate
5. Gather feedback

### Phase 3: Full Deployment (Production)
1. Deploy to production
2. Monitor continuously for first week
3. Track metrics daily
4. Adjust system prompt if needed
5. Optimize based on data

## Monitoring Queries

### Today's Call Activity
```sql
SELECT 
    DATEPART(HOUR, CreatedAt) AS Hour,
    COUNT(*) AS Calls,
    SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) AS Bookings
FROM VapiCallLogs
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
AND CreatedBy = 'VapiTimerFunction'
GROUP BY DATEPART(HOUR, CreatedAt)
ORDER BY Hour;
```

### Candidates Needing Second Call
```sql
SELECT 
    c.Name,
    c.EmailId,
    c.Phone,
    irf.Name AS JobTitle,
    vcl.CallStatus,
    vcl.CreatedAt AS LastCallDate
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
INNER JOIN VapiCallLogs vcl ON c.EmailId = vcl.RecipientEmail
WHERE vcl.InterviewBooked != 1 OR vcl.InterviewBooked IS NULL
AND (
    SELECT COUNT(*) 
    FROM VapiCallLogs 
    WHERE RecipientEmail = c.EmailId
) = 1
ORDER BY vcl.CreatedAt DESC;
```

## Expected Results

### Per Run (Every 15 minutes)
- **Candidates Processed**: Up to 10
- **Execution Time**: 20-60 seconds (depending on call count)
- **Successful Bookings**: Varies (typically 20-40% conversion rate)

### Per Day (Assuming 48 runs: 9 AM to 9 PM = 12 hours × 4 runs/hour)
- **Maximum Calls**: 480 calls (if always 10 eligible candidates)
- **Realistic Calls**: 50-200 calls (depending on candidate pool)
- **Expected Bookings**: 10-80 interviews scheduled

### Per Week
- **Maximum Calls**: ~2,400 calls (if running every day)
- **Realistic Calls**: 300-1,000 calls
- **Expected Bookings**: 60-400 interviews

## Cost Considerations

### Vapi Costs
- **Model**: gpt-5.2-instant (cost per call depends on Vapi pricing)
- **Voice**: 11labs or Deepgram (cost per minute)
- **Average Call Duration**: 2-5 minutes
- **Estimated Cost**: $0.10-0.50 per call (varies by provider and model)

### Azure Costs
- **Timer Function**: Very minimal (consumption plan)
- **Database**: Standard SQL Database costs
- **Bandwidth**: Minimal for API calls

### Monthly Estimate (1,000 calls/month)
- **Vapi**: $100-500
- **Azure**: $10-50
- **Total**: $110-550 (highly variable)

## Success Criteria

✅ Function runs on schedule  
✅ Candidates are called automatically  
✅ Call logs are created correctly  
✅ Webhook receives booking data  
✅ InterviewMeetings table updated via webhook  
✅ No candidate called more than twice  
✅ Function respects business hours  

## Next Steps After Deployment

1. **Week 1**: Monitor closely, adjust prompts if needed
2. **Week 2-4**: Analyze data, optimize timing and messaging
3. **Month 2+**: Fine-tune based on conversion rates
4. **Ongoing**: Regular monitoring and improvements

## Support & Maintenance

### Regular Tasks
- Monitor success rates weekly
- Review failed calls and adjust logic
- Update system prompt based on feedback
- Adjust operating hours if needed

### Alerts to Set Up
- Function failures (>5 per day)
- Success rate drops below 20%
- No eligible candidates for 24 hours
- Vapi API errors

---

**Implementation Date**: February 24, 2026  
**Status**: Ready for deployment  
**Estimated Setup Time**: 15-30 minutes  
