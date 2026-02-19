# Complete Implementation Guide: Vapi.ai Interview Booking

## 🎯 What You're Building

An automated voice AI system that:
- ☎️ Calls candidates on your behalf
- 🤖 Conducts natural conversations to book interviews
- 📅 Checks availability and schedules interviews
- ✉️ Sends confirmations automatically
- 📊 Tracks all interactions and analytics

## 📁 Files Created

```
Risebird/
├── Risebird.Entities/
│   └── Data/
│       ├── VapiCallLog.cs ✅ (Database entity)
│       └── VapiModels/
│           ├── VapiCallRequest.cs ✅
│           ├── VapiCallResponse.cs ✅
│           └── VapiWebhookMessage.cs ✅
│
├── Risebird.CommonHelper/
│   └── VapiService.cs ✅ (API integration)
│
├── Risebird.CommonHelper.Interface/
│   └── IVapiService.cs ✅
│
├── Risebird.Business/
│   └── VapiInterviewService.cs ✅ (Business logic)
│
├── Risebird.Business.Interface/
│   └── IVapiInterviewService.cs ✅
│
├── Risebird.Repository/
│   └── VapiCallRepository.cs ✅ (Data access)
│
├── Risebird.Repository.Interface/
│   └── IVapiCallRepository.cs ✅
│
├── Risebird.Web/
│   ├── Controllers/
│   │   └── VapiInterviewCallController.cs ✅
│   └── Views/
│       └── VapiInterviewCall/
│           └── Index.cshtml ✅
│
├── Risebird.DB/
│   ├── Tables/
│   │   └── VapiCallLogs.sql ✅
│   └── StoredProcedures/
│       ├── usp_GetPendingCandidatesForVapiCalls.sql ✅
│       └── usp_GetVapiCallStatistics.sql ✅
│
└── Documentation/
    ├── VAPI_INTEGRATION_README.md ✅
    ├── VAPI_QUICKSTART_GUIDE.md ✅
    ├── VapiIntegrationExamples.md ✅
    └── VapiConfigurationSnippet.json ✅
```

---

## 🚀 Implementation Steps (60 Minutes)

### Phase 1: Setup (15 minutes)

#### 1.1 Create Vapi Account
```bash
# Go to https://vapi.ai/
# Sign up with your email
# Verify email and login
# Get $10 free credits automatically
```

#### 1.2 Get API Key
1. Dashboard → Settings → API Keys
2. Click "Create New Key"
3. Copy the key (starts with `sk_live_...` or `sk_test_...`)

#### 1.3 Update Configuration
Add to `Risebird.Web/appsettings.json`:

```json
{
  "Vapi": {
    "ApiKey": "sk_live_your_actual_api_key_here",
    "BaseUrl": "https://api.vapi.ai",
    "InterviewBookingAssistantId": ""
  }
}
```

Also update `appsettings.Development.json` for local testing.

---

### Phase 2: Database Setup (10 minutes)

#### 2.1 Create Table

Open SQL Server Management Studio and execute:

```sql
-- From: Risebird.DB\Tables\VapiCallLogs.sql

CREATE TABLE [dbo].[VapiCallLogs]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [VapiCallId] NVARCHAR(100) NOT NULL UNIQUE,
    [InterviewId] INT NULL,
    [CandidateId] INT NULL,
    [CandidatePhone] NVARCHAR(20) NOT NULL,
    [CandidateName] NVARCHAR(255) NULL,
    [CandidateEmail] NVARCHAR(255) NULL,
    [CallStatus] NVARCHAR(50) NULL,
    [ScheduledDateTime] DATETIME NULL,
    [CallStartedAt] DATETIME NULL,
    [CallEndedAt] DATETIME NULL,
    [EndedReason] NVARCHAR(100) NULL,
    [DurationSeconds] INT NULL,
    [Transcript] NVARCHAR(MAX) NULL,
    [RecordingUrl] NVARCHAR(500) NULL,
    [InterviewBooked] BIT NULL,
    [BookedInterviewDateTime] DATETIME NULL,
    [BookingConfirmationDetails] NVARCHAR(MAX) NULL,
    [CallCost] DECIMAL(10, 4) NULL,
    [AssistantId] NVARCHAR(100) NULL,
    [Summary] NVARCHAR(MAX) NULL,
    [StructuredData] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME NULL,
    [CreatedBy] NVARCHAR(255) NULL,
    [UpdatedBy] NVARCHAR(255) NULL,
    
    INDEX [IX_VapiCallLogs_VapiCallId] NONCLUSTERED ([VapiCallId]),
    INDEX [IX_VapiCallLogs_InterviewId] NONCLUSTERED ([InterviewId]),
    INDEX [IX_VapiCallLogs_CandidateId] NONCLUSTERED ([CandidateId]),
    INDEX [IX_VapiCallLogs_CreatedAt] NONCLUSTERED ([CreatedAt] DESC)
);
```

#### 2.2 Create Stored Procedures

Execute both:
- `usp_GetPendingCandidatesForVapiCalls.sql`
- `usp_GetVapiCallStatistics.sql`

#### 2.3 Verify

```sql
-- Verify table created
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VapiCallLogs';

-- Verify stored procedures
SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME LIKE '%Vapi%';
```

---

### Phase 3: Code Integration (20 minutes)

#### 3.1 Register Services in Program.cs

Find your service registration section (usually after `var builder = WebApplication.CreateBuilder(args);`) and add:

```csharp
// Find this section in Program.cs:
// builder.Services.AddScoped<...>();

// Add these lines:
builder.Services.AddHttpClient(); // If not already added
builder.Services.AddScoped<IVapiService, VapiService>();
builder.Services.AddScoped<IVapiCallRepository, VapiCallRepository>();
builder.Services.AddScoped<IVapiInterviewService, VapiInterviewService>();
```

#### 3.2 Build Solution

```bash
# In Visual Studio: Ctrl+Shift+B
# Or via command line:
cd c:\Users\harsh\source\repos\Risebird
dotnet build
```

Fix any compilation errors if they occur.

#### 3.3 Verify Files Compiled

Check that all new files are included in the build. If any are missing:

```bash
# In each project directory
dotnet add reference ../Risebird.Entities/Risebird.Entities.csproj
# Repeat for other dependencies
```

---

### Phase 4: Assistant Configuration (10 minutes)

#### 4.1 Setup Local Testing (if needed)

If testing locally, setup ngrok:

```bash
# Install ngrok (if not installed)
choco install ngrok
# Or download from https://ngrok.com/download

# Start your application first
cd Risebird.Web
dotnet run

# In another terminal, create tunnel
ngrok http https://localhost:44360

# Copy the HTTPS URL (e.g., https://abc123.ngrok.io)
```

Update `appsettings.Development.json`:
```json
{
  "HostDetails": {
    "baseUrl": "https://abc123.ngrok.io"
  }
}
```

#### 4.2 Create Assistant via API

**Option A: Via UI** (Recommended)
1. Run your application
2. Navigate to: `https://localhost:44360/VapiInterviewCall`
3. Click **"Setup Assistant"** button
4. Copy the Assistant ID from response

**Option B: Via Postman/curl**

```bash
curl -X POST https://localhost:44360/VapiInterviewCall/SetupAssistant \
  -H "Content-Type: application/json" \
  -H "Cookie: your_auth_cookie_here"
```

#### 4.3 Save Assistant ID

Update `appsettings.json`:
```json
{
  "Vapi": {
    "InterviewBookingAssistantId": "asst_xxxxx_your_actual_id"
  }
}
```

---

### Phase 5: Testing (15 minutes)

#### 5.1 Test Call to Your Phone

```csharp
// Quick test in immediate window or test controller:
await _vapiService.InitiateCallAsync(new VapiCallRequest
{
    PhoneNumber = "+919999999999", // YOUR PHONE
    AssistantId = "asst_xxxxx...",
    AssistantOverrides = new VapiAssistantOverrides
    {
        VariableValues = new Dictionary<string, object>
        {
            { "candidateName", "Test User" },
            { "jobTitle", "Software Developer" },
            { "interviewId", 1 }
        }
    }
});
```

#### 5.2 Monitor the Call

Watch in:
1. **Your database**: `SELECT * FROM VapiCallLogs ORDER BY CreatedAt DESC`
2. **Vapi Dashboard**: https://dashboard.vapi.ai/calls
3. **Your application logs**: Check Application Insights or log files

#### 5.3 Test Workflow

During the call:
- ✅ AI introduces itself
- ✅ Asks for availability
- ✅ Offers time slots
- ✅ Books the interview
- ✅ Confirms details

After the call:
- ✅ Check database for call log
- ✅ Verify transcript saved
- ✅ Check if interview was marked as booked
- ✅ Listen to recording (if enabled)

#### 5.4 Test Webhook

Check your application logs for webhook events:
```
[Timestamp] Vapi Webhook: status-update - Call: call_xxxxx, Status: ringing
[Timestamp] Vapi Webhook: status-update - Call: call_xxxxx, Status: in-progress
[Timestamp] Vapi Webhook: transcript - Call: call_xxxxx, Role: assistant, Text: Hello!...
[Timestamp] Vapi Webhook: tool-calls - Call: call_xxxxx, Function: check_availability
[Timestamp] Vapi Webhook: end-of-call-report - Call: call_xxxxx, Duration: 150s
```

---

## 🔗 Integration with Existing Systems

### Connect to Your Calendar System

Update `VapiService.cs` → `CheckAvailability()`:

```csharp
private async Task<string> CheckAvailability(Dictionary<string, object> arguments)
{
    // Extract interview details from call metadata
    var interviewId = // get from call context
    
    // Query your actual calendar system
    // Example integrations:
    
    // Option 1: Calendly
    var slots = await _calendlyHelper.GetAvailableSlotsAsync(interviewId);
    
    // Option 2: Google Calendar  
    var slots = await _googleEventHelper.GetAvailableSlotsAsync(interviewId);
    
    // Option 3: Zoom
    var slots = await _zoomHelper.GetAvailableSlotsAsync(interviewId);
    
    // Option 4: Your custom table
    var slots = await _interviewRepository.GetAvailableTimeSlotsAsync(interviewId);
    
    return JsonConvert.SerializeObject(new
    {
        success = true,
        availableSlots = slots.Select(s => new
        {
            date = s.StartTime.ToString("yyyy-MM-dd"),
            time = s.StartTime.ToString("HH:mm"),
            interviewerName = s.InterviewerName
        })
    });
}
```

### Connect to Your Booking System

Update `VapiService.cs` → `BookInterview()`:

```csharp
private async Task<string> BookInterview(Dictionary<string, object> arguments)
{
    var interviewDateTime = DateTime.Parse(arguments["interviewDateTime"].ToString());
    var interviewId = // extract from call metadata
    var candidateId = // extract from call metadata
    
    // 1. Update your interview table
    await _interviewRepository.UpdateInterviewDateTimeAsync(interviewId, interviewDateTime);
    
    // 2. Update candidate status
    await _candidateRepository.UpdateStatusAsync(candidateId, "Interview Scheduled");
    
    // 3. Send confirmation email
    await _emailService.SendInterviewConfirmationAsync(new
    {
        CandidateEmail = // from metadata,
        InterviewDateTime = interviewDateTime,
        JobTitle = // from metadata,
        MeetingLink = // generate or retrieve
    });
    
    // 4. Notify interviewer
    await _notificationHelper.NotifyInterviewerAsync(interviewId);
    
    // 5. Create calendar event
    await _googleEventHelper.CreateEventAsync(interviewDateTime, interviewId);
    
    return JsonConvert.SerializeObject(new
    {
        success = true,
        bookingId = interviewId.ToString(),
        message = "Interview successfully booked"
    });
}
```

### Connect to Email Service

```csharp
// Add email tool to assistant
new
{
    type = "function",
    function = new
    {
        name = "send_confirmation_email",
        description = "Send interview confirmation to candidate",
        parameters = new
        {
            type = "object",
            properties = new
            {
                email = new { type = "string" },
                interviewDetails = new { type = "string" }
            }
        }
    },
    server = new { url = webhookUrl }
}

// Handler in VapiService.cs
private async Task<string> SendConfirmationEmail(Dictionary<string, object> arguments)
{
    var email = arguments["email"].ToString();
    var details = arguments["interviewDetails"].ToString();
    
    // Use your existing email service (SendGrid)
    await _sendGridService.SendEmailAsync(new
    {
        To = email,
        Subject = "Interview Confirmation - Risebird",
        Body = $"Your interview has been confirmed. Details: {details}",
        Template = "InterviewConfirmation"
    });
    
    return JsonConvert.SerializeObject(new { success = true });
}
```

---

## 🎨 UI/UX Integration

### Add to Existing Interview List View

Find your interview list view (e.g., `Views/Interview/Index.cshtml`) and add:

```cshtml
@* Add this column to your table *@
<th>AI Call</th>

@* Add this in the row *@
<td>
    @if (!string.IsNullOrEmpty(interview.CandidatePhone))
    {
        <button class="btn btn-sm btn-success" 
                onclick="callCandidate(@interview.InterviewId, '@interview.CandidateId', '@interview.CandidateName', '@interview.CandidateEmail', '@interview.CandidatePhone', '@interview.JobTitle')"
                title="Call candidate via AI">
            <i class="fa fa-phone"></i> Call
        </button>
        
        @* Show call status if already called *@
        @if (interview.VapiCallStatus != null)
        {
            <span class="badge badge-@GetVapiStatusBadge(interview.VapiCallStatus)">
                @interview.VapiCallStatus
            </span>
        }
    }
    else
    {
        <small class="text-muted">No phone</small>
    }
</td>

@section Scripts {
<script>
function callCandidate(interviewId, candidateId, name, email, phone, jobTitle) {
    if (!confirm(`Initiate AI call to ${name}?`)) return;
    
    $.ajax({
        url: '/VapiInterviewCall/InitiateInterviewCall',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            interviewId: interviewId,
            candidateId: candidateId,
            candidateName: name,
            candidateEmail: email,
            candidatePhone: phone,
            jobTitle: jobTitle
        }),
        success: function(response) {
            if (response.success) {
                toastr.success('AI call initiated! Call ID: ' + response.callId);
                setTimeout(() => location.reload(), 2000);
            } else {
                toastr.error('Failed: ' + response.message);
            }
        },
        error: function(xhr) {
            toastr.error('Error: ' + xhr.responseText);
        }
    });
}
</script>
}
```

### Add Bulk Call Button

```cshtml
@* Add above your table *@
<div class="mb-3">
    <button class="btn btn-primary" onclick="callSelectedCandidates()">
        <i class="fa fa-phone"></i> Call Selected Candidates
    </button>
    <button class="btn btn-info" onclick="callAllPending()">
        <i class="fa fa-phone-volume"></i> Call All Pending
    </button>
</div>

<script>
function callAllPending() {
    if (!confirm('This will call ALL candidates with pending interviews. Continue?')) return;
    
    $.post('/VapiInterviewCall/InitiateBatchCalls', { interviewId: @Model.InterviewId }, function(response) {
        if (response.success) {
            toastr.success(`Initiated ${response.totalCalls} calls`);
        }
    });
}
</script>
```

---

## 🔌 API Endpoint Reference

### Quick Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/VapiInterviewCall/InitiateInterviewCall` | POST | Start a call |
| `/VapiInterviewCall/GetCallStatus` | GET | Check call status |
| `/VapiInterviewCall/GetInterviewCalls` | GET | List calls for interview |
| `/VapiInterviewCall/Webhook` | POST | Receive Vapi webhooks |
| `/VapiInterviewCall/SetupAssistant` | POST | Create assistant |

### Usage Examples

#### Start Call

```javascript
// jQuery
$.ajax({
    url: '/VapiInterviewCall/InitiateInterviewCall',
    type: 'POST',
    contentType: 'application/json',
    data: JSON.stringify({
        interviewId: 123,
        candidateId: 456,
        candidateName: "John Doe",
        candidateEmail: "john@example.com",
        candidatePhone: "+919876543210",
        jobTitle: "Senior Developer"
    }),
    success: function(response) {
        console.log('Call started:', response.callId);
    }
});
```

```csharp
// C# HttpClient
var client = new HttpClient();
var payload = new
{
    interviewId = 123,
    candidateId = 456,
    candidateName = "John Doe",
    candidateEmail = "john@example.com",
    candidatePhone = "+919876543210",
    jobTitle = "Senior Developer"
};

var json = JsonConvert.SerializeObject(payload);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync(
    "https://portal.risebird.io/VapiInterviewCall/InitiateInterviewCall",
    content
);
```

#### Check Status

```javascript
// Poll for status updates
function pollCallStatus(callId) {
    const interval = setInterval(async () => {
        const response = await fetch(`/VapiInterviewCall/GetCallStatus?callId=${callId}`);
        const data = await response.json();
        
        console.log('Status:', data.vapiData.status);
        
        if (data.vapiData.status === 'ended') {
            clearInterval(interval);
            console.log('Call ended:', data.vapiData.endedReason);
        }
    }, 5000); // Every 5 seconds
}
```

---

## 📊 Dashboard Integration

### Add Analytics Widget to Dashboard

In your main dashboard view (e.g., `Views/Dashboard/Index.cshtml`):

```cshtml
<div class="row">
    <div class="col-md-12">
        <div class="card">
            <div class="card-header">
                <h4 class="card-title">
                    <i class="fa fa-phone"></i> AI Call Analytics
                    <small class="float-right">Last 7 days</small>
                </h4>
            </div>
            <div class="card-body">
                <div class="row" id="vapiStats">
                    <div class="col-md-3">
                        <div class="text-center">
                            <h3 id="vapi_total_calls">0</h3>
                            <p>Total Calls</p>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="text-center">
                            <h3 id="vapi_booked">0</h3>
                            <p>Interviews Booked</p>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="text-center">
                            <h3 id="vapi_conversion">0%</h3>
                            <p>Conversion Rate</p>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="text-center">
                            <h3 id="vapi_cost">$0.00</h3>
                            <p>Total Cost</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
$(document).ready(function() {
    loadVapiStats();
    setInterval(loadVapiStats, 60000); // Refresh every minute
});

function loadVapiStats() {
    $.get('/VapiInterviewCall/GetStatistics', function(data) {
        $('#vapi_total_calls').text(data.totalCalls);
        $('#vapi_booked').text(data.bookingsCompleted);
        $('#vapi_conversion').text(data.conversionRate.toFixed(1) + '%');
        $('#vapi_cost').text('$' + data.totalCost.toFixed(2));
    });
}
</script>
```

### Add Statistics Endpoint to Controller

```csharp
// Add to VapiInterviewCallController.cs
[HttpGet]
public async Task<IActionResult> GetStatistics(int days = 7)
{
    var startDate = DateTime.UtcNow.AddDays(-days);
    var stats = await _vapiInterviewService.GetCallStatisticsAsync(startDate, DateTime.UtcNow);
    
    return Json(new
    {
        totalCalls = stats.TotalCalls,
        bookingsCompleted = stats.BookingsCompleted,
        conversionRate = stats.BookingConversionRate,
        totalCost = stats.TotalCost
    });
}
```

---

## 🔄 Automated Workflows

### Automatic Call After Interview Creation

Add to your existing interview creation logic:

```csharp
// In your InterviewController or service
public async Task<IActionResult> CreateInterview(InterviewViewModel model)
{
    // Your existing code to create interview
    var interview = await _interviewService.CreateAsync(model);
    
    // ✨ NEW: Auto-initiate AI call
    if (model.AutoCallCandidate && !string.IsNullOrEmpty(model.CandidatePhone))
    {
        try
        {
            _ = Task.Run(async () =>
            {
                // Delay by 1 minute to allow database transaction to complete
                await Task.Delay(TimeSpan.FromMinutes(1));
                
                await _vapiService.InitiateCallAsync(new VapiCallRequest
                {
                    PhoneNumber = model.CandidatePhone,
                    AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
                    AssistantOverrides = new VapiAssistantOverrides
                    {
                        VariableValues = new Dictionary<string, object>
                        {
                            { "candidateName", model.CandidateName },
                            { "jobTitle", model.JobTitle },
                            { "interviewId", interview.InterviewId }
                        }
                    }
                });
            });
            
            TempData["Message"] = "Interview created and AI call scheduled";
        }
        catch (Exception ex)
        {
            // Don't fail interview creation if call fails
            await _logger.LogMessage(LogLevelInfo.Warning, ex);
        }
    }
    
    return RedirectToAction("Details", new { id = interview.InterviewId });
}
```

### Scheduled Daily Reminder Calls

Create a background job (using Hangfire, Azure Functions, or similar):

```csharp
// Azure Function or scheduled task
[FunctionName("SendDailyInterviewReminders")]
public async Task RunAsync([TimerTrigger("0 0 9 * * *")] TimerInfo timer) // 9 AM daily
{
    // Get interviews for tomorrow
    var tomorrow = DateTime.Today.AddDays(1);
    var interviews = await _interviewRepository.GetInterviewsByDateAsync(tomorrow);
    
    foreach (var interview in interviews)
    {
        // Check if already called in last 24 hours
        var recentCalls = await _vapiCallRepository.GetCallLogsByCandidateIdAsync(interview.CandidateId);
        if (recentCalls.Any(c => c.CreatedAt > DateTime.UtcNow.AddHours(-24)))
            continue;
        
        // Initiate reminder call
        await _vapiService.InitiateCallAsync(new VapiCallRequest
        {
            PhoneNumber = interview.CandidatePhone,
            AssistantId = _configuration["Vapi:ReminderAssistantId"],
            AssistantOverrides = new VapiAssistantOverrides
            {
                VariableValues = new Dictionary<string, object>
                {
                    { "candidateName", interview.CandidateName },
                    { "interviewTime", interview.InterviewDate.ToString("h:mm tt") },
                    { "interviewDate", interview.InterviewDate.ToString("MMMM dd") }
                }
            }
        });
        
        // Delay between calls
        await Task.Delay(TimeSpan.FromSeconds(30));
    }
}
```

---

## 📱 Mobile Responsiveness

### Mobile-Friendly Call Initiation

```cshtml
@* Mobile-optimized button *@
<div class="d-md-none">
    <a href="javascript:void(0)" 
       onclick="quickCall('@interview.CandidatePhone', '@interview.CandidateName')"
       class="btn btn-success btn-block">
        <i class="fa fa-phone"></i> Call @interview.CandidateName
    </a>
</div>

<script>
function quickCall(phone, name) {
    // Show loading state
    showLoader('Initiating call...');
    
    $.post('/VapiInterviewCall/QuickCall', { phone: phone, name: name }, function(response) {
        hideLoader();
        if (response.success) {
            showToast('Call started! They should receive a call shortly.', 'success');
        } else {
            showToast('Failed to start call: ' + response.message, 'error');
        }
    });
}
</script>
```

---

## 🎯 Performance Optimization

### Caching Strategy

```csharp
// Cache assistant configuration
public class VapiService : IVapiService
{
    private static readonly Dictionary<string, (string AssistantId, DateTime Expiry)> _assistantCache 
        = new Dictionary<string, (string, DateTime)>();
    
    private async Task<string> GetCachedAssistantIdAsync(string key)
    {
        if (_assistantCache.ContainsKey(key) && _assistantCache[key].Expiry > DateTime.UtcNow)
        {
            return _assistantCache[key].AssistantId;
        }
        
        // Create new assistant or get from config
        var assistantId = _configuration[$"Vapi:{key}"];
        if (string.IsNullOrEmpty(assistantId))
        {
            assistantId = await CreateInterviewBookingAssistantAsync(key, _webhookUrl);
        }
        
        _assistantCache[key] = (assistantId, DateTime.UtcNow.AddHours(24));
        return assistantId;
    }
}
```

### Batch Processing

```csharp
// Process calls in batches to avoid overwhelming system
public async Task ProcessPendingCallsAsync()
{
    const int BATCH_SIZE = 10;
    const int DELAY_BETWEEN_BATCHES_MS = 5000;
    
    var pendingCandidates = await _repository.GetPendingCandidatesForCallsAsync();
    
    for (int i = 0; i < pendingCandidates.Count; i += BATCH_SIZE)
    {
        var batch = pendingCandidates.Skip(i).Take(BATCH_SIZE).ToList();
        
        var tasks = batch.Select(candidate => 
            _vapiService.InitiateCallAsync(CreateCallRequest(candidate))
        );
        
        await Task.WhenAll(tasks);
        
        // Delay between batches
        if (i + BATCH_SIZE < pendingCandidates.Count)
        {
            await Task.Delay(DELAY_BETWEEN_BATCHES_MS);
        }
    }
}
```

---

## 🐛 Debugging Tips

### Enable Detailed Logging

```csharp
// Add to VapiService.cs
private async Task<T> ExecuteWithLoggingAsync<T>(Func<Task<T>> action, string operationName)
{
    var startTime = DateTime.UtcNow;
    
    try
    {
        await _logger.LogMessage(LogLevelInfo.Information, 
            new Exception($"[Vapi] Starting: {operationName}"));
        
        var result = await action();
        
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        await _logger.LogMessage(LogLevelInfo.Information, 
            new Exception($"[Vapi] Completed: {operationName} in {duration}ms"));
        
        return result;
    }
    catch (Exception ex)
    {
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        await _logger.LogMessage(LogLevelInfo.Exception, 
            new Exception($"[Vapi] Failed: {operationName} after {duration}ms", ex));
        throw;
    }
}

// Usage
public async Task<VapiCallResponse> InitiateCallAsync(VapiCallRequest request)
{
    return await ExecuteWithLoggingAsync(
        () => InitiateCallInternalAsync(request),
        $"InitiateCall to {request.PhoneNumber}"
    );
}
```

### Test Webhook Handler Locally

```csharp
// Add test endpoint
[HttpPost]
[Route("TestWebhook")]
public async Task<IActionResult> TestWebhookLocally()
{
    var mockMessage = new VapiWebhookMessage
    {
        Message = new VapiMessageContent
        {
            Type = "tool-calls",
            CallId = "test_call_123",
            ToolCalls = new VapiToolCalls
            {
                ToolCalls = new List<VapiToolCall>
                {
                    new VapiToolCall
                    {
                        Id = "test_001",
                        Type = "function",
                        Function = new VapiToolFunction
                        {
                            Name = "check_availability",
                            Arguments = new Dictionary<string, object>
                            {
                                { "preferredDate", "2026-02-05" }
                            }
                        }
                    }
                }
            }
        }
    };
    
    var result = await _vapiService.ProcessToolCallAsync(mockMessage);
    return Json(result);
}
```

---

## 📞 Voice & Language Options

### Available Voices

```csharp
// Premium voices (11labs)
var premiumVoices = new Dictionary<string, string>
{
    // Female
    { "sarah", "Professional, warm" },
    { "melissa", "Friendly, energetic" },
    { "andrea", "Confident, clear" },
    { "marissa", "Calm, soothing" },
    
    // Male
    { "burt", "Deep, authoritative" },
    { "phillip", "Professional, mature" },
    { "steve", "Friendly, approachable" },
    { "joseph", "Warm, conversational" }
};

// Fast & affordable (Deepgram)
var deepgramVoices = new Dictionary<string, string>
{
    { "asteria", "Female, neutral" },
    { "luna", "Female, warm" },
    { "stella", "Female, energetic" },
    { "athena", "Female, professional" },
    { "zeus", "Male, authoritative" },
    { "orion", "Male, friendly" }
};
```

### Multi-Language Configuration

```csharp
// Create language-specific assistants
public async Task<string> CreateHindiAssistantAsync()
{
    var assistant = new
    {
        name = "Hindi Interview Scheduler",
        transcriber = new
        {
            provider = "deepgram",
            model = "nova-2",
            language = "hi"
        },
        model = new
        {
            provider = "openai",
            model = "gpt-4o",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = @"आप Risebird से एक भर्ती सहायक हैं। 
                    आपको इंटरव्यू बुक करना है।
                    विनम्र और पेशेवर रहें।"
                }
            }
        },
        voice = new
        {
            provider = "11labs",
            voiceId = "your_hindi_voice_id"
        },
        firstMessage = "नमस्ते! मैं Risebird से बोल रही हूं।"
    };
    
    // Create assistant...
}
```

---

## 💡 Best Practices

### 1. Phone Number Validation

```csharp
public bool IsValidPhoneNumber(string phone)
{
    // E.164 format validation
    var pattern = @"^\+[1-9]\d{1,14}$";
    return System.Text.RegularExpressions.Regex.IsMatch(phone, pattern);
}

// Usage before calling
if (!IsValidPhoneNumber(candidatePhone))
{
    // Normalize or reject
    candidatePhone = NormalizePhoneNumber(candidatePhone);
}
```

### 2. Rate Limiting

```csharp
private static readonly SemaphoreSlim _callSemaphore = new SemaphoreSlim(5, 5); // Max 5 concurrent calls

public async Task<VapiCallResponse> InitiateCallAsync(VapiCallRequest request)
{
    await _callSemaphore.WaitAsync();
    
    try
    {
        return await InitiateCallInternalAsync(request);
    }
    finally
    {
        _callSemaphore.Release();
    }
}
```

### 3. Error Recovery

```csharp
public async Task<VapiCallResponse> InitiateCallWithRetryAsync(VapiCallRequest request, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await _vapiService.InitiateCallAsync(request);
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries) throw;
            
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
        }
    }
    
    throw new Exception("All retry attempts failed");
}
```

### 4. Cost Monitoring

```csharp
// Add cost check before initiating calls
public async Task<bool> CheckBudgetBeforeCallAsync()
{
    var stats = await _vapiInterviewService.GetCallStatisticsAsync(
        DateTime.UtcNow.Date.AddDays(-30), 
        DateTime.UtcNow
    );
    
    var monthlyBudget = _configuration.GetValue<double>("Vapi:Cost:MonthlyBudget");
    var alertThreshold = _configuration.GetValue<double>("Vapi:Cost:AlertThreshold");
    
    if (stats.TotalCost >= monthlyBudget)
    {
        await _notificationHelper.SendAlertAsync("Vapi monthly budget exceeded!");
        return false;
    }
    
    if (stats.TotalCost >= alertThreshold)
    {
        await _notificationHelper.SendWarningAsync($"Vapi costs at {stats.TotalCost:C} (threshold: {alertThreshold:C})");
    }
    
    return true;
}
```

---

## 🎓 Training Your Team

### Admin User Guide

Create a simple guide for your team:

**How to Make a Call:**
1. Open interview in system
2. Click "AI Call" button next to candidate
3. System will call them automatically
4. Check status in "Vapi Calls" dashboard
5. Review transcript after call ends

**How to Check Results:**
1. Go to Dashboard → Vapi Calls
2. Filter by date, interview, or candidate
3. Click "View" to see transcript
4. Green badge = Interview booked
5. Red badge = Need follow-up

**What to Do If Call Fails:**
- Check phone number is correct
- Manually call candidate as backup
- Review transcript to understand why
- Update candidate preferences

---

## 🚀 Launch Checklist

### Pre-Launch (Do Once)

- [ ] Vapi account created
- [ ] API key configured in production
- [ ] Database table created
- [ ] Stored procedures created
- [ ] Services registered in DI
- [ ] Assistant created and ID saved
- [ ] Webhook URL configured
- [ ] Test call completed successfully
- [ ] Team trained on dashboard

### Per Call Setup

- [ ] Candidate has valid phone number
- [ ] Interview details are correct
- [ ] Time zone is set correctly
- [ ] Email is valid for confirmations

### Monitoring (Weekly)

- [ ] Check call success rate (target: >70%)
- [ ] Review failed calls
- [ ] Monitor costs (stay under budget)
- [ ] Read transcripts for quality
- [ ] Update prompts if needed
- [ ] Gather candidate feedback

---

## 📊 Success Metrics

### Key Performance Indicators

Track these metrics weekly:

```sql
-- Weekly KPI Report
DECLARE @StartDate DATE = DATEADD(week, -1, GETDATE());
DECLARE @EndDate DATE = GETDATE();

SELECT 
    'Total Calls Attempted' as Metric,
    COUNT(*) as Value
FROM VapiCallLogs
WHERE CreatedAt BETWEEN @StartDate AND @EndDate

UNION ALL

SELECT 
    'Successful Connections',
    SUM(CASE WHEN CallStatus = 'ended' AND EndedReason NOT IN ('no-answer', 'busy') THEN 1 ELSE 0 END)
FROM VapiCallLogs
WHERE CreatedAt BETWEEN @StartDate AND @EndDate

UNION ALL

SELECT 
    'Interviews Booked',
    SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END)
FROM VapiCallLogs
WHERE CreatedAt BETWEEN @StartDate AND @EndDate

UNION ALL

SELECT 
    'Conversion Rate %',
    CAST(SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0) AS DECIMAL(5,1))
FROM VapiCallLogs
WHERE CreatedAt BETWEEN @StartDate AND @EndDate

UNION ALL

SELECT 
    'Total Cost USD',
    SUM(CAST(CallCost AS FLOAT))
FROM VapiCallLogs
WHERE CreatedAt BETWEEN @StartDate AND @EndDate;
```

### Target Benchmarks

| Metric | Target | Industry Avg |
|--------|--------|--------------|
| Connection Rate | >80% | 60-70% |
| Booking Conversion | >50% | 30-40% |
| Avg Call Duration | 2-4 min | 3-5 min |
| Cost per Booking | <$1.00 | $0.50-$1.50 |
| Candidate Satisfaction | >4.0/5 | 3.5/5 |

---

## 🎬 Complete Example Workflow

### End-to-End Scenario

```csharp
// 1. New candidate applies
public async Task<IActionResult> CandidateApplied(int candidateId, int jobId)
{
    var candidate = await _candidateService.GetByIdAsync(candidateId);
    var job = await _jobService.GetByIdAsync(jobId);
    
    // 2. System screens candidate
    var screeningResult = await _aiScreener.ScreenCandidateAsync(candidate, job);
    
    if (screeningResult.Passed)
    {
        // 3. Create interview
        var interview = await _interviewService.CreateAsync(new Interview
        {
            CandidateId = candidateId,
            JobId = jobId,
            Status = "Pending Scheduling"
        });
        
        // 4. Initiate AI call to book interview
        var callResponse = await _vapiService.InitiateCallAsync(new VapiCallRequest
        {
            PhoneNumber = candidate.MobileNumber,
            AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
            AssistantOverrides = new VapiAssistantOverrides
            {
                VariableValues = new Dictionary<string, object>
                {
                    { "candidateName", candidate.CandidateName },
                    { "jobTitle", job.Title },
                    { "interviewId", interview.InterviewId },
                    { "candidateId", candidate.CandidateId }
                }
            }
        });
        
        // 5. Log the call
        await _vapiCallRepository.SaveCallLogAsync(new VapiCallLog
        {
            VapiCallId = callResponse.Id,
            InterviewId = interview.InterviewId,
            CandidateId = candidate.CandidateId,
            CandidatePhone = candidate.MobileNumber,
            CandidateName = candidate.CandidateName,
            CallStatus = callResponse.Status,
            CreatedAt = DateTime.UtcNow
        });
        
        return Json(new { 
            success = true, 
            message = "Candidate screened and AI call initiated",
            callId = callResponse.Id 
        });
    }
    
    return Json(new { success = false, message = "Candidate did not pass screening" });
}

// 6. Webhook receives booking confirmation
// (Handled automatically by VapiInterviewCallController.Webhook)

// 7. System sends confirmation email
// (Handled in BookInterview tool function)

// 8. Interview appears in calendar
// (Integrated with your calendar service)
```

---

## 🎁 Bonus Features

### SMS Fallback

```csharp
// If call fails, automatically send SMS
public async Task HandleFailedCallAsync(string callId)
{
    var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
    
    if (callLog.EndedReason == "no-answer" || callLog.EndedReason == "busy")
    {
        // Send SMS as fallback
        await _twilioHelper.SendSmsAsync(
            callLog.CandidatePhone,
            $"Hi {callLog.CandidateName}, we tried calling you about your interview. " +
            $"Please call us back or visit: https://portal.risebird.io/interview/{callLog.InterviewId}"
        );
    }
}
```

### WhatsApp Integration

```csharp
// Send WhatsApp message after successful booking
public async Task SendWhatsAppConfirmationAsync(string callId)
{
    var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
    
    if (callLog.InterviewBooked == true)
    {
        await _whatsAppHelper.SendMessageAsync(
            callLog.CandidatePhone,
            $"✅ Interview Confirmed\n\n" +
            $"Date: {callLog.BookedInterviewDateTime:MMM dd, yyyy}\n" +
            $"Time: {callLog.BookedInterviewDateTime:h:mm tt}\n" +
            $"Position: [Job Title]\n\n" +
            $"Meeting link will be sent closer to the date."
        );
    }
}
```

### Sentiment Analysis

```csharp
// Analyze candidate sentiment from transcript
public async Task AnalyzeCallSentimentAsync(string callId)
{
    var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
    
    if (!string.IsNullOrEmpty(callLog.Transcript))
    {
        var sentiment = await _openAIService.AnalyzeSentimentAsync(callLog.Transcript);
        
        // Store in database or trigger actions
        if (sentiment.Score < 0.3) // Negative sentiment
        {
            await _notificationHelper.AlertAsync(
                $"Candidate {callLog.CandidateName} had negative sentiment in call. Manual follow-up recommended."
            );
        }
    }
}
```

---

## 🎉 You're All Set!

Your Vapi.ai integration is now complete! Here's what you can do:

### Immediate Actions:
1. ✅ Make your first test call
2. ✅ Review the transcript
3. ✅ Check the analytics dashboard
4. ✅ Adjust assistant personality if needed

### This Week:
1. 📞 Call 10-20 candidates
2. 📊 Monitor success rate
3. 📝 Gather feedback
4. 🔧 Fine-tune prompts

### This Month:
1. 🚀 Scale to full candidate pool
2. 📈 Track ROI (time saved vs. cost)
3. 🎯 Optimize conversion rate
4. 🌟 Train team on best practices

**Questions? Check the documentation:**
- `VAPI_INTEGRATION_README.md` - Complete reference
- `VAPI_QUICKSTART_GUIDE.md` - Quick start
- `VapiIntegrationExamples.md` - Code examples

**Happy calling! 🎊**
