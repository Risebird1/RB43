# Testing Your Vapi Integration

## 🧪 Validation Checklist

Use this checklist to verify your integration is working correctly.

---

## Pre-Flight Checks

### ✅ Configuration
```bash
# Verify appsettings.json has:
- Vapi:ApiKey (not empty)
- Vapi:BaseUrl (https://api.vapi.ai)
- HostDetails:baseUrl (your domain)
```

### ✅ Database
```sql
-- Verify table exists
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'VapiCallLogs';
-- Should return: 1

-- Verify stored procedures
SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_NAME LIKE '%Vapi%';
-- Should return: 2 or more
```

### ✅ Services Registered
```csharp
// Check Program.cs has:
builder.Services.AddScoped<IVapiService, VapiService>();
builder.Services.AddScoped<IVapiCallRepository, VapiCallRepository>();
builder.Services.AddScoped<IVapiInterviewService, VapiInterviewService>();
```

### ✅ Build Successful
```bash
cd Risebird.Web
dotnet build
# Should complete without errors
```

---

## Test Suite

### Test 1: Create Assistant ⭐

**Purpose**: Verify API connectivity and assistant creation

**Steps**:
1. Run application: `dotnet run`
2. Navigate to: `https://localhost:44360/VapiInterviewCall`
3. Click "Setup Assistant" button

**Expected Result**:
```json
{
  "success": true,
  "assistantId": "asst_xxxxxxxxxxxxxxxxxxxxxxxx",
  "message": "Assistant created successfully..."
}
```

**If Failed**:
- Check API key in appsettings.json
- Verify Vapi account has credits
- Check internet connectivity

---

### Test 2: Initiate Test Call ⭐⭐

**Purpose**: Verify end-to-end call flow

**Steps**:
1. Update Assistant ID in appsettings.json
2. Navigate to: `/VapiInterviewCall`
3. Click "Initiate New Call"
4. Fill in your details:
   - Interview ID: `1`
   - Candidate ID: `1`
   - Your name
   - Your email
   - **Your phone number** (with country code: +91...)
   - Job Title: "Test Position"
5. Click "Start Call"

**Expected Result**:
1. You receive a call within 10-30 seconds
2. AI introduces itself
3. Conversation flows naturally
4. Database updated with call log

**Verify**:
```sql
SELECT TOP 1 * FROM VapiCallLogs ORDER BY CreatedAt DESC;
-- Should show your test call
```

**If Failed**:
- Check phone number format (+country code)
- Verify webhook URL is accessible
- Check application logs
- Review Vapi dashboard for errors

---

### Test 3: Webhook Reception ⭐⭐

**Purpose**: Verify webhook events are received

**Steps**:
1. During a call, monitor application logs
2. Check for webhook events

**Expected Log Entries**:
```
[INFO] Vapi Webhook: status-update, Status: ringing
[INFO] Vapi Webhook: status-update, Status: in-progress
[INFO] Vapi Webhook: transcript, Text: Hello! This is...
[INFO] Vapi Webhook: end-of-call-report, Duration: 45s
```

**Verify in Database**:
```sql
SELECT VapiCallId, CallStatus, CallStartedAt, CallEndedAt 
FROM VapiCallLogs 
ORDER BY CreatedAt DESC;
```

**If Failed**:
- Webhook URL not accessible (use ngrok for local testing)
- Firewall blocking incoming requests
- SSL certificate issues

---

### Test 4: Tool Call Execution ⭐⭐⭐

**Purpose**: Verify function calling works

**During Test Call**:
- When AI asks about availability, provide a date
- AI should call `check_availability` function
- You should hear available time slots

**Expected Behavior**:
1. AI asks: "When would be good for you?"
2. You say: "Next Tuesday"
3. AI: "Let me check available slots..." (calls tool)
4. AI: "We have 10 AM, 2 PM, and 4 PM available..."

**Verify in Logs**:
```
[INFO] Vapi Webhook: tool-calls, Function: check_availability
[INFO] Tool call result sent to Vapi
```

**If Failed**:
- Check `ProcessToolCallAsync` method
- Verify tool definitions in assistant
- Check webhook is returning proper JSON

---

### Test 5: Interview Booking ⭐⭐⭐

**Purpose**: Verify booking flow works end-to-end

**During Test Call**:
- Confirm a time slot
- Complete the booking

**Expected Behavior**:
1. You select a time: "2 PM works for me"
2. AI confirms: "Tuesday at 2 PM, correct?"
3. You: "Yes"
4. AI: "Great! Booking now..." (calls book_interview)
5. AI: "Interview booked! You'll get confirmation email"

**Verify**:
```sql
SELECT * FROM VapiCallLogs 
WHERE InterviewBooked = 1 
ORDER BY CreatedAt DESC;
-- Should show your booking
```

**If Failed**:
- Check `BookInterview` method implementation
- Verify database permissions
- Check email service configuration

---

### Test 6: Call Analytics ⭐

**Purpose**: Verify analytics are calculated correctly

**Steps**:
```sql
-- Run statistics procedure
EXEC usp_GetVapiCallStatistics;
```

**Expected Output**:
- Result set 1: Overall stats
- Result set 2: Calls by status
- Result set 3: Calls by ended reason
- Result set 4: Daily trends
- Result set 5: Recent calls

**If Failed**:
- Verify procedure was created
- Check data exists in VapiCallLogs
- Review procedure syntax

---

### Test 7: UI Dashboard ⭐

**Purpose**: Verify UI is functional

**Steps**:
1. Navigate to `/VapiInterviewCall`
2. Should see list of calls
3. Click "View" on a call
4. Modal should show call details

**Expected Display**:
- Call list table populated
- Status badges colored correctly
- Actions buttons functional
- Modals open properly

**If Failed**:
- Check JavaScript console for errors
- Verify jQuery is loaded
- Check route configuration

---

## Performance Tests

### Test 8: Batch Calling

**Purpose**: Verify system can handle multiple calls

**Steps**:
```csharp
var candidates = Enumerable.Range(1, 10).Select(i => new CandidateCallInfo
{
    InterviewId = i,
    CandidateId = i,
    CandidateName = $"Test Candidate {i}",
    CandidateEmail = $"test{i}@example.com",
    CandidatePhone = $"+9199999999{i:00}",
    JobTitle = "Test Position"
}).ToList();

var results = await _vapiInterviewService.InitiateBatchCallsAsync(candidates);
Console.WriteLine($"Successfully initiated {results.Count} out of 10 calls");
```

**Expected**:
- All 10 calls initiated
- No timeout errors
- Database has 10 new rows

---

### Test 9: Load Test

**Purpose**: Verify system under load

**Using JMeter or similar**:
1. Send 50 concurrent call requests
2. Monitor response times
3. Check error rate

**Targets**:
- Response time: <2 seconds
- Error rate: <1%
- All calls logged correctly

---

## Security Tests

### Test 10: Webhook Authentication

**Purpose**: Verify webhook security

**Steps**:
```bash
# Test without auth (should be blocked in production)
curl -X POST https://your-domain.com/VapiInterviewCall/Webhook \
  -H "Content-Type: application/json" \
  -d '{"message":{"type":"test"}}'
```

**Implementation** (add to controller):
```csharp
// Verify webhook source
var vapiIPs = new[] { "35.171.100.50", "44.234.148.54" }; // Vapi IPs
var clientIP = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

if (!vapiIPs.Contains(clientIP))
{
    return Unauthorized();
}
```

---

### Test 11: Data Privacy

**Purpose**: Verify sensitive data handling

**Checks**:
- [ ] API key not in source control
- [ ] No credentials in logs
- [ ] Transcripts encrypted at rest
- [ ] Recordings auto-deleted (if configured)
- [ ] PII redacted (if configured)

---

## Integration Tests

### Test 12: Calendar Integration

```csharp
[Test]
public async Task TestCalendarAvailability()
{
    var arguments = new Dictionary<string, object>
    {
        { "preferredDate", "2026-02-05" }
    };
    
    var result = await _vapiService.CheckAvailability(arguments);
    var data = JsonConvert.DeserializeObject<dynamic>(result);
    
    Assert.IsTrue(data.success);
    Assert.IsNotNull(data.availableSlots);
    Assert.Greater(data.availableSlots.Count, 0);
}
```

### Test 13: Email Integration

```csharp
[Test]
public async Task TestConfirmationEmail()
{
    var callLog = new VapiCallLog
    {
        CandidateEmail = "test@example.com",
        CandidateName = "Test User",
        BookedInterviewDateTime = DateTime.Now.AddDays(1)
    };
    
    var result = await _emailService.SendInterviewConfirmationAsync(callLog);
    
    Assert.IsTrue(result);
    // Check email was sent (via email service logs)
}
```

---

## Regression Tests

### Test 14: End-to-End Flow

**Complete Workflow Test**:

1. Create test candidate in database
2. Create test interview
3. Initiate AI call via API
4. Interact with AI (answer call)
5. Book an interview time
6. Verify in database:
   ```sql
   SELECT * FROM VapiCallLogs WHERE CandidateId = @TestCandidateId;
   SELECT * FROM Interviews WHERE InterviewId = @TestInterviewId;
   ```
7. Check email received
8. Verify calendar event created

**Pass Criteria**:
- ✅ Call completed successfully
- ✅ Interview marked as booked
- ✅ Database updated correctly
- ✅ Email sent
- ✅ Calendar event created

---

## Monitoring & Alerts

### Test 15: Alert System

**Setup Alerts For**:

1. **Failed Calls >10% in 1 hour**
```csharp
var recentCalls = await _repository.GetCallsSince(DateTime.UtcNow.AddHours(-1));
var failureRate = recentCalls.Count(c => c.CallStatus == "failed") * 100.0 / recentCalls.Count;

if (failureRate > 10)
{
    await _alertService.SendAlert("High call failure rate: " + failureRate + "%");
}
```

2. **Budget Exceeded**
```csharp
var monthlyCost = await _repository.GetMonthlyCost();
var budget = _configuration.GetValue<double>("Vapi:Cost:MonthlyBudget");

if (monthlyCost > budget)
{
    await _alertService.SendAlert($"Budget exceeded: ${monthlyCost:F2} / ${budget:F2}");
}
```

3. **Webhook Failures**
```csharp
// Log webhook failures
catch (Exception ex)
{
    await _alertService.SendAlert("Webhook processing failed: " + ex.Message);
    throw;
}
```

---

## Test Results Template

```markdown
# Vapi Integration Test Results
Date: [Date]
Tester: [Name]

## Configuration Tests
- [ ] API key configured: PASS/FAIL
- [ ] Database table created: PASS/FAIL
- [ ] Services registered: PASS/FAIL
- [ ] Build successful: PASS/FAIL

## Functional Tests
- [ ] Assistant created: PASS/FAIL
- [ ] Test call completed: PASS/FAIL
- [ ] Webhooks received: PASS/FAIL
- [ ] Tools executed: PASS/FAIL
- [ ] Booking completed: PASS/FAIL
- [ ] Analytics working: PASS/FAIL
- [ ] UI functional: PASS/FAIL

## Performance Tests
- [ ] Batch calling: PASS/FAIL
- [ ] Response time acceptable: PASS/FAIL
- [ ] No memory leaks: PASS/FAIL

## Security Tests
- [ ] Webhook auth working: PASS/FAIL
- [ ] No credentials exposed: PASS/FAIL
- [ ] Data encrypted: PASS/FAIL

## Integration Tests
- [ ] Calendar connected: PASS/FAIL
- [ ] Email sending: PASS/FAIL
- [ ] Database updates: PASS/FAIL

## Overall Status: PASS / FAIL
Issues Found: [List any issues]
Notes: [Any additional notes]
```

---

## Automated Test Script

### Run All Tests

```csharp
// Create a test controller or console app
public class VapiIntegrationTests
{
    private readonly IVapiService _vapiService;
    private readonly IVapiCallRepository _repository;
    
    public async Task<TestResults> RunAllTestsAsync()
    {
        var results = new TestResults();
        
        // Test 1: Configuration
        results.Add("Configuration", await TestConfigurationAsync());
        
        // Test 2: Database
        results.Add("Database", await TestDatabaseAsync());
        
        // Test 3: API Connectivity
        results.Add("API Connectivity", await TestApiConnectivityAsync());
        
        // Test 4: Assistant Creation
        results.Add("Assistant Creation", await TestAssistantCreationAsync());
        
        // Test 5: Call Initiation
        results.Add("Call Initiation", await TestCallInitiationAsync());
        
        return results;
    }
    
    private async Task<bool> TestConfigurationAsync()
    {
        try
        {
            var apiKey = _configuration["Vapi:ApiKey"];
            var baseUrl = _configuration["Vapi:BaseUrl"];
            var hostUrl = _configuration["HostDetails:baseUrl"];
            
            return !string.IsNullOrEmpty(apiKey) 
                && !string.IsNullOrEmpty(baseUrl) 
                && !string.IsNullOrEmpty(hostUrl);
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> TestDatabaseAsync()
    {
        try
        {
            var recentCalls = await _repository.GetRecentCallLogsAsync(1);
            return true; // If no exception, database is accessible
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> TestApiConnectivityAsync()
    {
        try
        {
            var assistants = await _vapiService.ListAssistantsAsync();
            return !string.IsNullOrEmpty(assistants);
        }
        catch
        {
            return false;
        }
    }
    
    // ... more tests
}

public class TestResults
{
    private Dictionary<string, bool> _results = new Dictionary<string, bool>();
    
    public void Add(string testName, bool passed)
    {
        _results[testName] = passed;
        Console.WriteLine($"[{(passed ? "✅" : "❌")}] {testName}");
    }
    
    public bool AllPassed => _results.All(r => r.Value);
    
    public string Summary => 
        $"Tests: {_results.Count}, Passed: {_results.Count(r => r.Value)}, Failed: {_results.Count(r => !r.Value)}";
}
```

---

## Manual Testing Script

### Interactive Test Session

```powershell
# PowerShell script for manual testing

Write-Host "🧪 Vapi Integration Test Suite" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check Configuration
Write-Host "Test 1: Configuration Check" -ForegroundColor Yellow
$apiKey = (Get-Content appsettings.json | ConvertFrom-Json).Vapi.ApiKey
if ($apiKey) {
    Write-Host "✅ API Key configured" -ForegroundColor Green
} else {
    Write-Host "❌ API Key missing" -ForegroundColor Red
}

# Test 2: Database
Write-Host "`nTest 2: Database Check" -ForegroundColor Yellow
$dbCheck = Invoke-Sqlcmd -Query "SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VapiCallLogs'" -ServerInstance "your_server" -Database "your_db"
if ($dbCheck.Count -eq 1) {
    Write-Host "✅ Database table exists" -ForegroundColor Green
} else {
    Write-Host "❌ Database table missing" -ForegroundColor Red
}

# Test 3: API Connectivity
Write-Host "`nTest 3: API Connectivity" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://api.vapi.ai/assistant" -Headers @{Authorization="Bearer $apiKey"} -Method GET
    Write-Host "✅ API accessible" -ForegroundColor Green
} catch {
    Write-Host "❌ API not accessible: $_" -ForegroundColor Red
}

# Test 4: Webhook Accessibility
Write-Host "`nTest 4: Webhook Check" -ForegroundColor Yellow
$webhookUrl = (Get-Content appsettings.json | ConvertFrom-Json).HostDetails.baseUrl + "/VapiInterviewCall/Webhook"
try {
    $response = Invoke-WebRequest -Uri $webhookUrl -Method POST -Body '{"test":true}' -ContentType "application/json"
    Write-Host "✅ Webhook accessible" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Webhook may not be accessible externally" -ForegroundColor Yellow
}

Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "Test Suite Complete!" -ForegroundColor Cyan
```

---

## Common Issues & Solutions

### Issue: "401 Unauthorized"
**Cause**: Invalid API key  
**Solution**: 
1. Verify API key in Vapi dashboard
2. Check appsettings.json
3. Ensure no extra spaces or quotes

### Issue: "Phone number invalid"
**Cause**: Wrong format  
**Solution**: Use E.164 format (+[country][number])
```csharp
// Correct
"+919876543210"

// Fix common issues
phone = phone.Replace(" ", "").Replace("-", "");
if (!phone.StartsWith("+")) phone = "+91" + phone;
```

### Issue: "Webhook not received"
**Cause**: URL not accessible  
**Solution**:
1. For local testing, use ngrok
2. For production, ensure HTTPS
3. Check firewall rules
4. Verify DNS resolution

### Issue: "Assistant not responding"
**Cause**: Model configuration issue  
**Solution**:
1. Check OpenAI credits in Vapi dashboard
2. Verify model name is correct
3. Review assistant logs in Vapi dashboard

### Issue: "Tool calls timing out"
**Cause**: Webhook handler slow  
**Solution**:
1. Increase timeout: `timeoutSeconds: 30`
2. Optimize database queries
3. Use async operations
4. Add caching

---

## Verification Queries

### Quick Health Check

```sql
-- Overall system health
SELECT 
    'Total Calls' as Metric,
    COUNT(*) as Value
FROM VapiCallLogs

UNION ALL

SELECT 
    'Last 24 Hours',
    COUNT(*)
FROM VapiCallLogs
WHERE CreatedAt >= DATEADD(hour, -24, GETUTCDATE())

UNION ALL

SELECT 
    'Success Rate %',
    CAST(SUM(CASE WHEN CallStatus = 'ended' AND EndedReason NOT IN ('no-answer', 'failed') THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0) AS DECIMAL(5,1))
FROM VapiCallLogs
WHERE CreatedAt >= DATEADD(day, -7, GETUTCDATE())

UNION ALL

SELECT 
    'Booking Rate %',
    CAST(SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0) AS DECIMAL(5,1))
FROM VapiCallLogs
WHERE CreatedAt >= DATEADD(day, -7, GETUTCDATE())

UNION ALL

SELECT 
    'Avg Duration (sec)',
    AVG(CAST(DurationSeconds AS FLOAT))
FROM VapiCallLogs
WHERE DurationSeconds IS NOT NULL;
```

---

## Post-Deployment Validation

### Day 1 Checklist
- [ ] First call successful
- [ ] Webhook events received
- [ ] Call logged in database
- [ ] Transcript saved
- [ ] No errors in logs

### Week 1 Checklist
- [ ] 10+ calls completed
- [ ] 60%+ success rate
- [ ] Analytics working
- [ ] Team trained
- [ ] No major issues

### Month 1 Checklist
- [ ] 100+ calls completed
- [ ] 65%+ booking rate
- [ ] Cost under budget
- [ ] Calendar integrated
- [ ] Process optimized

---

## Test Data Generator

### Create Test Candidates

```sql
-- Insert test candidates for testing (use with caution!)
INSERT INTO Candidate (CandidateName, CandidateEmail, MobileNumber, CreatedAt)
VALUES 
('Test Candidate 1', 'test1@example.com', '+919999999901', GETUTCDATE()),
('Test Candidate 2', 'test2@example.com', '+919999999902', GETUTCDATE()),
('Test Candidate 3', 'test3@example.com', '+919999999903', GETUTCDATE());

-- Create test interviews
INSERT INTO Interviews (CandidateId, IRFId, InterviewDate, InterviewStatus, CreatedAt)
SELECT 
    CandidateId, 
    1, -- Replace with valid IRF ID
    DATEADD(day, 2, GETUTCDATE()), 
    1, -- Pending
    GETUTCDATE()
FROM Candidate 
WHERE CandidateEmail LIKE 'test%@example.com';
```

---

## Success Confirmation

### ✅ Integration is Ready When:

1. **Configuration**
   - API key configured ✓
   - Assistant ID saved ✓
   - Webhook URL working ✓

2. **Database**
   - Table created ✓
   - Stored procedures working ✓
   - Indexes created ✓

3. **Functionality**
   - Test call successful ✓
   - Webhooks received ✓
   - Tools executed ✓
   - Booking confirmed ✓

4. **UI/UX**
   - Dashboard loads ✓
   - Buttons functional ✓
   - Data displays correctly ✓

5. **Documentation**
   - Team trained ✓
   - Guides available ✓
   - Support contacts known ✓

---

## 🎉 Ready for Production!

If all tests pass, you're ready to:

1. **Deploy to production**
2. **Start calling real candidates**
3. **Monitor performance**
4. **Iterate and improve**

**Good luck! 🚀**

---

## Test Report Template

```
# Vapi Integration Test Report

**Date**: [Date]
**Environment**: [Dev/Staging/Production]
**Tester**: [Name]

## Test Summary
- Total Tests: [X]
- Passed: [X]
- Failed: [X]
- Skipped: [X]

## Detailed Results
1. Configuration: ✅/❌
2. Database: ✅/❌
3. API Connectivity: ✅/❌
4. Assistant Creation: ✅/❌
5. Call Initiation: ✅/❌
6. Webhook Reception: ✅/❌
7. Tool Execution: ✅/❌
8. Booking Flow: ✅/❌
9. Analytics: ✅/❌
10. UI Dashboard: ✅/❌

## Issues Found
[List any issues]

## Recommendations
[List recommendations]

## Sign-off
Ready for Production: YES / NO

Approved by: _______________
Date: _______________
```

---

**Testing complete? Move forward with confidence! 💪**
