# Vapi.ai Quick Start Guide

## 🚀 Get Started in 10 Minutes

### Step 1: Sign Up for Vapi (2 minutes)

1. Go to [vapi.ai](https://vapi.ai/)
2. Click "Sign Up" and create an account
3. Navigate to Dashboard → Settings → API Keys
4. Click "Create New Key" and copy it
5. You'll get $10 free credits to start

### Step 2: Configure Your Application (2 minutes)

Add to `appsettings.json`:

```json
{
  "Vapi": {
    "ApiKey": "sk_live_your_api_key_here",
    "BaseUrl": "https://api.vapi.ai",
    "InterviewBookingAssistantId": ""
  },
  "HostDetails": {
    "baseUrl": "https://portal.risebird.io"
  }
}
```

### Step 3: Register Services (1 minute)

Update your `Program.cs`:

```csharp
// Add after other service registrations
builder.Services.AddHttpClient();
builder.Services.AddScoped<IVapiService, VapiService>();
builder.Services.AddScoped<IVapiCallRepository, VapiCallRepository>();
builder.Services.AddScoped<IVapiInterviewService, VapiInterviewService>();
```

### Step 4: Create Database Table (1 minute)

Run this SQL script in SQL Server Management Studio:

```sql
-- Copy and execute from: Risebird.DB\Tables\VapiCallLogs.sql
-- Or run via sqlcmd:
-- sqlcmd -S your_server -d your_database -i "Risebird.DB\Tables\VapiCallLogs.sql"
```

### Step 5: Setup Assistant (2 minutes)

1. Build and run your application
2. Navigate to: `https://localhost:44360/VapiInterviewCall`
3. Click **"Setup Assistant"** button
4. Copy the Assistant ID from the response
5. Update `appsettings.json`:
   ```json
   "Vapi": {
     "InterviewBookingAssistantId": "asst_xxxxx..."
   }
   ```

### Step 6: Make Your First Call (2 minutes)

In the Vapi dashboard:

1. Click **"Initiate New Call"**
2. Fill in:
   - Interview ID: `1` (test)
   - Candidate ID: `1` (test)
   - Name: `Test Candidate`
   - Email: `test@example.com`
   - Phone: `+919999999999` (use your real number for testing)
   - Job Title: `Software Developer`
3. Click **"Start Call"**
4. Answer the call and interact with the AI!

---

## 📞 Making Calls from Code

### Option 1: Direct API Call

```csharp
// In any controller or service
var response = await _vapiService.InitiateCallAsync(new VapiCallRequest
{
    PhoneNumber = "+919876543210",
    AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
    AssistantOverrides = new VapiAssistantOverrides
    {
        VariableValues = new Dictionary<string, object>
        {
            { "candidateName", "John Doe" },
            { "jobTitle", "Senior Developer" },
            { "interviewId", 123 }
        }
    }
});

Console.WriteLine($"Call initiated! ID: {response.Id}, Status: {response.Status}");
```

### Option 2: Via Business Service

```csharp
// Batch calling multiple candidates
var candidates = new List<CandidateCallInfo>
{
    new CandidateCallInfo
    {
        InterviewId = 100,
        CandidateId = 1,
        CandidateName = "Alice Smith",
        CandidateEmail = "alice@example.com",
        CandidatePhone = "+919876543210",
        JobTitle = "Frontend Developer"
    },
    new CandidateCallInfo
    {
        InterviewId = 101,
        CandidateId = 2,
        CandidateName = "Bob Johnson",
        CandidateEmail = "bob@example.com",
        CandidatePhone = "+919876543211",
        JobTitle = "Backend Developer"
    }
};

var callLogs = await _vapiInterviewService.InitiateBatchCallsAsync(candidates);
Console.WriteLine($"Initiated {callLogs.Count} calls");
```

---

## 🔧 Customization Checklist

### Customize the AI Voice

```csharp
// Available voices (in VapiService.cs):
// Female: sarah, melissa, andrea, marissa, paula
// Male: burt, phillip, steve, joseph, ryan, drew

voice = new
{
    provider = "11labs",
    voiceId = "burt", // Change to your preferred voice
    speed = 1.0 // Adjust speaking speed (0.5 - 2.0)
}
```

### Customize the Conversation

Edit system prompt in `VapiService.cs` → `CreateInterviewBookingAssistantAsync()`:

```csharp
content = @"You are [YOUR CUSTOMIZATION HERE]

Guidelines:
- [Your guideline 1]
- [Your guideline 2]
..."
```

### Customize Available Time Slots

Update `CheckAvailability()` method in `VapiService.cs`:

```csharp
private string CheckAvailability(Dictionary<string, object> arguments)
{
    // Replace mock data with real calendar query
    var slots = await _yourCalendarService.GetSlotsAsync(...);
    
    return JsonConvert.SerializeObject(new { 
        success = true, 
        availableSlots = slots 
    });
}
```

---

## 🐛 Troubleshooting

### "Call failed to connect"
**Cause**: Phone number format incorrect  
**Fix**: Ensure number is in E.164 format: `+[country code][number]`
```csharp
// Correct
"+919876543210"  // India
"+14155551234"   // USA
"+442071234567"  // UK

// Incorrect
"9876543210"     // Missing country code
"+91 98765 43210" // Has spaces
```

### "Assistant not found"
**Cause**: Assistant ID not configured  
**Fix**: Run the SetupAssistant endpoint and save the ID

### "Webhook not receiving calls"
**Cause**: URL not accessible  
**Fix**: 
- Ensure HTTPS
- Test URL accessibility: `curl https://your-domain.com/VapiInterviewCall/Webhook`
- For local testing, use ngrok

### "No response from tool calls"
**Cause**: Tool call handler not returning proper JSON  
**Fix**: Ensure all tool handlers return valid JSON string

---

## 💰 Cost Calculator

```csharp
public double CalculateCallCost(int durationSeconds)
{
    const double TRANSCRIPTION_PER_MIN = 0.0055;
    const double LLM_PER_MIN = 0.003;
    const double VOICE_PER_MIN = 0.06;
    const double TRANSPORT_PER_MIN = 0.01;
    
    var minutes = durationSeconds / 60.0;
    
    return (TRANSCRIPTION_PER_MIN + LLM_PER_MIN + VOICE_PER_MIN + TRANSPORT_PER_MIN) * minutes;
}

// Example usage:
var cost = CalculateCallCost(180); // 3 minute call
Console.WriteLine($"Cost: ${cost:F2}"); // Output: Cost: $0.21
```

### Monthly Budget Planning

| Scenario | Calls/Month | Avg Duration | Monthly Cost |
|----------|-------------|--------------|--------------|
| Small | 100 | 3 min | ~$24 |
| Medium | 500 | 3 min | ~$120 |
| Large | 2000 | 3 min | ~$480 |

---

## 📊 Monitoring Dashboard

### Real-time Call Monitoring

```javascript
// Add to your view
function monitorActiveCalls() {
    $.get('/VapiInterviewCall/GetActiveCalls', function(calls) {
        $('#activeCallsCount').text(calls.length);
        
        var html = '';
        calls.forEach(function(call) {
            html += `
                <div class="call-item">
                    <strong>${call.candidateName}</strong>
                    <span class="badge badge-info">${call.status}</span>
                    <small>${call.durationSeconds}s</small>
                </div>
            `;
        });
        $('#activeCallsList').html(html);
    });
}

// Poll every 10 seconds
setInterval(monitorActiveCalls, 10000);
```

---

## 🎯 Common Use Cases

### Use Case 1: Interview Confirmation Call

```csharp
// Call candidates to confirm scheduled interviews
var interviewsToConfirm = await _interviewRepository
    .GetUnconfirmedInterviewsAsync(DateTime.Now.AddDays(1));

foreach (var interview in interviewsToConfirm)
{
    await _vapiService.InitiateCallAsync(new VapiCallRequest
    {
        PhoneNumber = interview.CandidatePhone,
        AssistantId = _configuration["Vapi:ConfirmationAssistantId"],
        AssistantOverrides = new VapiAssistantOverrides
        {
            VariableValues = new Dictionary<string, object>
            {
                { "candidateName", interview.CandidateName },
                { "interviewDateTime", interview.InterviewDate.ToString("MMMM dd, yyyy at h:mm tt") },
                { "interviewerName", interview.InterviewerName }
            }
        }
    });
}
```

### Use Case 2: Follow-up After No-Show

```csharp
// Automatically call candidates who missed interviews
var noShows = await _interviewRepository.GetNoShowInterviewsAsync();

foreach (var noShow in noShows)
{
    // Wait 2 hours after missed interview
    if (DateTime.Now - noShow.ScheduledTime < TimeSpan.FromHours(2))
        continue;
        
    await _vapiService.InitiateCallAsync(new VapiCallRequest
    {
        PhoneNumber = noShow.CandidatePhone,
        AssistantId = _configuration["Vapi:FollowUpAssistantId"],
        AssistantOverrides = new VapiAssistantOverrides
        {
            VariableValues = new Dictionary<string, object>
            {
                { "candidateName", noShow.CandidateName },
                { "missedInterviewTime", noShow.ScheduledTime.ToString("h:mm tt") },
                { "rescheduleOptions", true }
            }
        }
    });
}
```

### Use Case 3: Availability Check Before Scheduling

```csharp
// Call to check availability before creating interview
public async Task<IActionResult> CheckCandidateAvailability(int candidateId)
{
    var candidate = await _candidateService.GetByIdAsync(candidateId);
    
    var response = await _vapiService.InitiateCallAsync(new VapiCallRequest
    {
        PhoneNumber = candidate.MobileNumber,
        AssistantId = _configuration["Vapi:AvailabilityCheckAssistantId"],
        AssistantOverrides = new VapiAssistantOverrides
        {
            VariableValues = new Dictionary<string, object>
            {
                { "candidateName", candidate.CandidateName },
                { "purpose", "availability_check" }
            }
        }
    });
    
    return Json(new { success = true, callId = response.Id });
}
```

---

## 🔐 Security Configuration

### Webhook Authentication

```csharp
// Add to VapiInterviewCallController.cs
[HttpPost]
[AllowAnonymous]
public async Task<IActionResult> Webhook([FromBody] VapiWebhookMessage message)
{
    // Verify webhook signature
    var signature = Request.Headers["X-Vapi-Signature"].FirstOrDefault();
    var rawBody = await new StreamReader(Request.Body).ReadToEndAsync();
    
    if (!VerifyWebhookSignature(rawBody, signature))
    {
        return Unauthorized();
    }
    
    // Process webhook...
}

private bool VerifyWebhookSignature(string payload, string signature)
{
    // Implement HMAC verification if Vapi provides webhook secret
    // For now, you can also check IP whitelist or use API key
    
    var vapiApiKey = _configuration["Vapi:ApiKey"];
    // Add your verification logic here
    
    return true; // Placeholder
}
```

### Store API Keys Securely

```bash
# Use User Secrets for development
dotnet user-secrets set "Vapi:ApiKey" "your_api_key_here"

# Use Azure Key Vault for production
# In Program.cs:
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential()
);
```

---

## 📈 Analytics Query Examples

### Daily Call Report

```sql
-- Daily call statistics
SELECT 
    CAST(CreatedAt AS DATE) as CallDate,
    COUNT(*) as TotalCalls,
    SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) as BookingsCompleted,
    AVG(DurationSeconds) as AvgDurationSeconds,
    SUM(CallCost) as TotalCost
FROM VapiCallLogs
WHERE CreatedAt >= DATEADD(day, -30, GETDATE())
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY CallDate DESC;
```

### Conversion Funnel

```sql
-- Call to booking conversion funnel
SELECT 
    COUNT(*) as TotalAttempts,
    SUM(CASE WHEN CallStatus = 'ended' THEN 1 ELSE 0 END) as Connected,
    SUM(CASE WHEN CallStatus = 'ended' AND EndedReason NOT IN ('no-answer', 'busy') THEN 1 ELSE 0 END) as Conversations,
    SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) as Bookings,
    CAST(SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0) AS DECIMAL(5,2)) as ConversionRate
FROM VapiCallLogs
WHERE CreatedAt >= DATEADD(day, -7, GETDATE());
```

### Top Declined Reasons

```sql
-- Analyze why bookings failed
SELECT 
    EndedReason,
    COUNT(*) as Count,
    AVG(DurationSeconds) as AvgDuration
FROM VapiCallLogs
WHERE InterviewBooked = 0 
  AND CallStatus = 'ended'
  AND CreatedAt >= DATEADD(day, -30, GETDATE())
GROUP BY EndedReason
ORDER BY Count DESC;
```

---

## 🧪 Testing Guide

### Test with Your Own Phone

```csharp
// Test endpoint - calls your phone
[HttpGet]
public async Task<IActionResult> TestMyPhone()
{
    var myPhone = "+919999999999"; // Your phone number
    
    var response = await _vapiService.InitiateCallAsync(new VapiCallRequest
    {
        PhoneNumber = myPhone,
        AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
        AssistantOverrides = new VapiAssistantOverrides
        {
            VariableValues = new Dictionary<string, object>
            {
                { "candidateName", "Test User" },
                { "jobTitle", "Test Position" },
                { "interviewId", 999 }
            }
        }
    });
    
    return Json(new { 
        success = true, 
        message = "Check your phone! Call ID: " + response.Id 
    });
}
```

### Test Webhook Locally

```bash
# Terminal 1: Start your app
dotnet run --project Risebird.Web

# Terminal 2: Start ngrok
ngrok http https://localhost:44360

# Copy the ngrok URL (e.g., https://abc123.ngrok.io)
# Update webhook in Vapi dashboard or assistant config
```

### Postman Test Collection

Create a Postman collection with these requests:

**1. Initiate Call**
```
POST /VapiInterviewCall/InitiateInterviewCall
Content-Type: application/json

{
  "interviewId": 1,
  "candidateId": 1,
  "candidateName": "Test Candidate",
  "candidateEmail": "test@example.com",
  "candidatePhone": "+919999999999",
  "jobTitle": "Developer"
}
```

**2. Get Call Status**
```
GET /VapiInterviewCall/GetCallStatus?callId=call_xxxxx
```

**3. Simulate Webhook (for testing)**
```
POST /VapiInterviewCall/Webhook
Content-Type: application/json

{
  "message": {
    "type": "tool-calls",
    "callId": "test_call_123",
    "toolCalls": {
      "toolCalls": [
        {
          "id": "call_test_001",
          "type": "function",
          "function": {
            "name": "check_availability",
            "arguments": {
              "preferredDate": "2026-02-05"
            }
          }
        }
      ]
    }
  }
}
```

---

## 🎨 UI Integration

### Add Call Button to Existing Interview Dashboard

```cshtml
@* In your existing interview list view *@
<table class="table">
    <thead>
        <tr>
            <th>Candidate</th>
            <th>Position</th>
            <th>Status</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var interview in Model)
        {
            <tr>
                <td>@interview.CandidateName</td>
                <td>@interview.JobTitle</td>
                <td>@interview.Status</td>
                <td>
                    @* Existing buttons *@
                    <button class="btn btn-sm btn-primary" onclick="editInterview(@interview.Id)">
                        Edit
                    </button>
                    
                    @* NEW: AI Call button *@
                    <button class="btn btn-sm btn-success" 
                            onclick="initiateAICall(@interview.Id, '@interview.CandidateName', '@interview.CandidatePhone', '@interview.JobTitle')"
                            title="Call candidate to book interview">
                        <i class="fa fa-phone"></i> AI Call
                    </button>
                </td>
            </tr>
        }
    </tbody>
</table>

<script>
function initiateAICall(interviewId, candidateName, candidatePhone, jobTitle) {
    if (!confirm(`Call ${candidateName} for ${jobTitle} position?`)) return;
    
    $.ajax({
        url: '/VapiInterviewCall/InitiateInterviewCall',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            interviewId: interviewId,
            candidateId: 0, // Will be populated from interview
            candidateName: candidateName,
            candidateEmail: '', // Will be populated from interview
            candidatePhone: candidatePhone,
            jobTitle: jobTitle
        }),
        success: function(response) {
            if (response.success) {
                showNotification('Success', 'AI call initiated! Call ID: ' + response.callId, 'success');
            } else {
                showNotification('Error', response.message, 'danger');
            }
        }
    });
}
</script>
```

---

## 📋 Production Deployment Steps

### 1. Environment Variables

```bash
# Set in Azure App Service Configuration

Vapi__ApiKey=sk_live_your_production_key
Vapi__BaseUrl=https://api.vapi.ai
Vapi__InterviewBookingAssistantId=asst_xxxxx
HostDetails__baseUrl=https://portal.risebird.io
```

### 2. Database Migration

```sql
-- Run in production database
-- Backup first!
BACKUP DATABASE [YourDatabase] TO DISK = 'path/to/backup.bak';

-- Then execute
-- c:\Users\harsh\source\repos\Risebird\Risebird.DB\Tables\VapiCallLogs.sql
```

### 3. Webhook URL Configuration

```bash
# Ensure your production URL is accessible
# Format: https://portal.risebird.io/VapiInterviewCall/Webhook

# Test webhook accessibility
curl -X POST https://portal.risebird.io/VapiInterviewCall/Webhook \
  -H "Content-Type: application/json" \
  -d '{"message":{"type":"test"}}'
```

### 4. Assistant Configuration

Run once in production:
```bash
# Navigate to
https://portal.risebird.io/VapiInterviewCall/SetupAssistant

# Save the returned Assistant ID to configuration
```

### 5. Monitor Initial Calls

```sql
-- Watch first few calls closely
SELECT TOP 10 
    VapiCallId,
    CandidateName,
    CallStatus,
    DurationSeconds,
    InterviewBooked,
    CreatedAt
FROM VapiCallLogs
ORDER BY CreatedAt DESC;
```

---

## 🚨 Common Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| `401 Unauthorized` | Invalid API key | Check Vapi:ApiKey in config |
| `400 Bad Request` | Invalid phone format | Use E.164: +[country][number] |
| `429 Too Many Requests` | Rate limit exceeded | Add delays between calls |
| `500 Internal Server Error` | Webhook handler error | Check logs and fix handler |
| Call not connecting | Phone number issue | Verify number is valid |
| No voicemail detection | Not configured | Enable in assistant config |

---

## 🎓 Learning Resources

- **Vapi Docs**: https://docs.vapi.ai/
- **API Reference**: https://docs.vapi.ai/api-reference
- **Quickstart Guide**: https://docs.vapi.ai/quickstart/phone
- **Community Discord**: https://discord.gg/vapi
- **Example Apps**: https://github.com/VapiAI/examples

---

## ✅ Success Checklist

Before going live:

- [ ] API key configured
- [ ] Assistant created and tested
- [ ] Database table created
- [ ] Webhook endpoint accessible via HTTPS
- [ ] Test call completed successfully
- [ ] Calendar integration working
- [ ] Email confirmations sending
- [ ] Call logs being saved
- [ ] Analytics dashboard functional
- [ ] Error handling tested
- [ ] Team trained on dashboard
- [ ] Monitoring alerts configured
- [ ] Budget limits set
- [ ] Privacy compliance reviewed

---

## 🆘 Support

**Issues with Vapi Platform:**
- Email: support@vapi.ai
- Discord: https://discord.gg/vapi
- Status: https://status.vapi.ai

**Issues with Integration:**
- Check `VAPI_INTEGRATION_README.md`
- Check `VapiIntegrationExamples.md`
- Review Application Insights logs
- Check Vapi dashboard for call logs

---

## 📞 Example Call Transcript

Here's what a successful call looks like:

```
🤖 AI: "Hello! This is Sarah from Risebird. May I speak with John Doe?"

👤 Candidate: "Yes, speaking."

🤖 AI: "Hi John! I'm calling about the Senior Developer position you applied for. 
       We'd love to schedule an interview with you. Do you have a moment?"

👤 Candidate: "Sure, go ahead."

🤖 AI: "Great! When would be a good time for you next week?"

👤 Candidate: "I'm available Tuesday or Wednesday afternoon."

🤖 AI: "Perfect, let me check our available slots."
      [Checks calendar via tool]
      "We have Tuesday at 2 PM and Wednesday at 3 PM available. Which works better?"

👤 Candidate: "Tuesday at 2 PM works for me."

🤖 AI: "Excellent! Let me confirm - Tuesday, February 4th at 2:00 PM. Is that correct?"

👤 Candidate: "Yes, that's perfect."

🤖 AI: [Books interview via tool]
      "Wonderful! Your interview is booked for Tuesday, February 4th at 2:00 PM. 
       You'll receive a confirmation email shortly with the meeting link. 
       Is there anything else you'd like to know?"

👤 Candidate: "No, that's all. Thank you!"

🤖 AI: "You're very welcome, John! We look forward to speaking with you on Tuesday. 
       Good luck with your preparation, and have a great day!"

👤 Candidate: "Thanks, you too! Bye."

🤖 AI: "Goodbye!"
      [Call ends]
```

**Metrics from this call:**
- Duration: 2 minutes 15 seconds
- Cost: ~$0.18
- Outcome: Interview successfully booked ✅
- Candidate satisfaction: High (based on tone analysis)

---

## 🎉 Next Steps

1. **Test with 5-10 candidates** to validate setup
2. **Analyze transcripts** to improve prompts
3. **Monitor costs** for first week
4. **Gather feedback** from candidates
5. **Optimize** based on results
6. **Scale up** to full candidate pool

**Ready to revolutionize your interview scheduling? Let's get started! 🚀**
