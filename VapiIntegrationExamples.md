# Vapi.ai Integration Examples

## Table of Contents
1. [Basic Call Example](#basic-call-example)
2. [Batch Calling Example](#batch-calling-example)
3. [Custom Tool Implementation](#custom-tool-implementation)
4. [Advanced Assistant Configuration](#advanced-assistant-configuration)
5. [Multi-language Support](#multi-language-support)
6. [Analytics Dashboard](#analytics-dashboard)

---

## Basic Call Example

### Single Candidate Call

```csharp
// In your controller or service
public async Task<IActionResult> CallCandidate(int candidateId)
{
    // Get candidate details from database
    var candidate = await _candidateService.GetCandidateByIdAsync(candidateId);
    var interview = await _interviewService.GetInterviewForCandidateAsync(candidateId);
    
    var callRequest = new VapiCallRequest
    {
        PhoneNumber = candidate.MobileNumber, // Must be E.164 format
        AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
        AssistantOverrides = new VapiAssistantOverrides
        {
            VariableValues = new Dictionary<string, object>
            {
                { "candidateName", candidate.CandidateName },
                { "jobTitle", interview.JobTitle },
                { "companyName", "Risebird" },
                { "interviewId", interview.InterviewId }
            }
        }
    };
    
    var response = await _vapiService.InitiateCallAsync(callRequest);
    
    return Json(new { success = true, callId = response.Id });
}
```

---

## Batch Calling Example

### Call Multiple Candidates at Once

```csharp
public async Task<IActionResult> CallPendingCandidates(int interviewId)
{
    // Get all candidates for this interview who haven't been called yet
    var pendingCandidates = await _interviewRepository
        .GetPendingCandidatesForInterviewAsync(interviewId);
    
    var candidateCallInfos = pendingCandidates.Select(c => new CandidateCallInfo
    {
        InterviewId = interviewId,
        CandidateId = c.CandidateId,
        CandidateName = c.CandidateName,
        CandidateEmail = c.CandidateEmail,
        CandidatePhone = c.MobileNumber,
        JobTitle = c.JobTitle
    }).ToList();
    
    // Initiate batch calls (with rate limiting built-in)
    var callLogs = await _vapiInterviewService.InitiateBatchCallsAsync(candidateCallInfos);
    
    return Json(new 
    { 
        success = true, 
        totalCalls = callLogs.Count,
        message = $"Initiated {callLogs.Count} calls successfully"
    });
}
```

### Scheduled Batch Calling

```csharp
// Background job or scheduled task
public async Task ScheduledBatchCallJobAsync()
{
    // Get interviews scheduled for tomorrow
    var tomorrow = DateTime.Today.AddDays(1);
    var upcomingInterviews = await _interviewRepository
        .GetInterviewsByDateRangeAsync(tomorrow, tomorrow.AddDays(1));
    
    foreach (var interview in upcomingInterviews)
    {
        // Get candidates who haven't confirmed
        var unconfirmedCandidates = await _interviewRepository
            .GetUnconfirmedCandidatesAsync(interview.InterviewId);
        
        if (unconfirmedCandidates.Any())
        {
            var callInfos = unconfirmedCandidates.Select(c => new CandidateCallInfo
            {
                InterviewId = interview.InterviewId,
                CandidateId = c.CandidateId,
                CandidateName = c.CandidateName,
                CandidateEmail = c.CandidateEmail,
                CandidatePhone = c.MobileNumber,
                JobTitle = interview.JobTitle,
                PreferredDateTime = interview.InterviewDate
            }).ToList();
            
            await _vapiInterviewService.InitiateBatchCallsAsync(callInfos);
            
            // Wait between interview batches
            await Task.Delay(5000);
        }
    }
}
```

---

## Custom Tool Implementation

### Calendar Integration Tool

```csharp
// In VapiService.cs - Add to CreateInterviewBookingAssistantAsync

// 1. Add check_calendar_availability tool
new
{
    type = "function",
    function = new
    {
        name = "check_calendar_availability",
        description = "Check interviewer's calendar availability for scheduling",
        parameters = new
        {
            type = "object",
            properties = new
            {
                interviewerId = new
                {
                    type = "integer",
                    description = "ID of the interviewer"
                },
                startDate = new
                {
                    type = "string",
                    description = "Start date for availability check (YYYY-MM-DD)"
                },
                endDate = new
                {
                    type = "string",
                    description = "End date for availability check (YYYY-MM-DD)"
                },
                duration = new
                {
                    type = "integer",
                    description = "Duration of interview in minutes"
                }
            },
            required = new[] { "interviewerId", "startDate", "duration" }
        }
    },
    server = new { url = webhookUrl }
}

// 2. Implement the handler
private async Task<string> CheckCalendarAvailability(Dictionary<string, object> arguments)
{
    var interviewerId = Convert.ToInt32(arguments["interviewerId"]);
    var startDate = DateTime.Parse(arguments["startDate"].ToString());
    var endDate = arguments.ContainsKey("endDate") 
        ? DateTime.Parse(arguments["endDate"].ToString()) 
        : startDate.AddDays(7);
    var duration = Convert.ToInt32(arguments["duration"]);
    
    // Query your calendar system (Google Calendar, Outlook, etc.)
    var availableSlots = await _calendarService.GetAvailableSlotsAsync(
        interviewerId, 
        startDate, 
        endDate, 
        duration
    );
    
    return JsonConvert.SerializeObject(new
    {
        success = true,
        availableSlots = availableSlots.Select(slot => new
        {
            date = slot.StartTime.ToString("yyyy-MM-dd"),
            time = slot.StartTime.ToString("HH:mm"),
            endTime = slot.EndTime.ToString("HH:mm"),
            interviewerName = slot.InterviewerName
        })
    });
}
```

### Email Confirmation Tool

```csharp
// Add to tools array
new
{
    type = "function",
    function = new
    {
        name = "send_interview_confirmation",
        description = "Send interview confirmation email to candidate",
        parameters = new
        {
            type = "object",
            properties = new
            {
                candidateEmail = new
                {
                    type = "string",
                    description = "Candidate's email address"
                },
                interviewDateTime = new
                {
                    type = "string",
                    description = "Confirmed interview date and time"
                },
                interviewLink = new
                {
                    type = "string",
                    description = "Meeting link (Zoom, Teams, etc.)"
                }
            },
            required = new[] { "candidateEmail", "interviewDateTime" }
        }
    },
    server = new { url = webhookUrl }
}

// Handler implementation
private async Task<string> SendInterviewConfirmation(Dictionary<string, object> arguments)
{
    var email = arguments["candidateEmail"].ToString();
    var interviewDateTime = arguments["interviewDateTime"].ToString();
    var meetingLink = arguments.ContainsKey("interviewLink") 
        ? arguments["interviewLink"].ToString() 
        : null;
    
    await _emailService.SendInterviewConfirmationAsync(new
    {
        To = email,
        Subject = "Interview Confirmation - Risebird",
        InterviewDateTime = interviewDateTime,
        MeetingLink = meetingLink
    });
    
    return JsonConvert.SerializeObject(new
    {
        success = true,
        message = "Confirmation email sent successfully"
    });
}
```

---

## Advanced Assistant Configuration

### Personality Customization

```csharp
var customAssistant = new
{
    name = "Friendly Interview Scheduler",
    model = new
    {
        provider = "openai",
        model = "gpt-4o", // Use GPT-4o for better conversation quality
        temperature = 0.8, // More creative/natural
        maxTokens = 300,
        messages = new[]
        {
            new
            {
                role = "system",
                content = @"You are Emma, a warm and professional recruitment coordinator at Risebird.

PERSONALITY:
- Friendly but professional
- Patient and understanding
- Good listener
- Empathetic to candidate's schedule constraints
- Clear communicator

CONVERSATION STYLE:
- Use natural, conversational language
- Don't sound scripted or robotic
- Show enthusiasm about the opportunity
- Be respectful of candidate's time
- Handle objections gracefully

GUIDELINES:
1. Start with a friendly greeting
2. Confirm identity politely
3. Build quick rapport (1-2 exchanges)
4. Explain the opportunity briefly
5. Listen actively to availability concerns
6. Offer flexible options
7. Confirm details carefully
8. End on positive note

FLEXIBILITY RULES:
- If candidate is busy, offer to call back
- If candidate seems hesitant, probe gently
- If they decline, thank them professionally
- If they need time to think, schedule follow-up

Remember: Your goal is to book the interview, but candidate experience is paramount."
            }
        }
    },
    voice = new
    {
        provider = "11labs",
        voiceId = "sarah",
        stability = 0.5,
        similarityBoost = 0.75,
        speed = 0.95 // Slightly slower for clarity
    },
    firstMessage = "Hi! This is Emma calling from Risebird. Is this {{candidateName}}?",
    
    // Advanced configurations
    backgroundSound = "office",
    
    startSpeakingPlan = new
    {
        waitSeconds = 0.5, // Wait before responding
        smartEndpointingEnabled = true
    },
    
    stopSpeakingPlan = new
    {
        numWords = 2, // Interrupt after 2 words
        voiceSeconds = 0.3
    },
    
    maxDurationSeconds = 600, // 10 minutes max
    
    endCallPhrases = new[] 
    { 
        "goodbye", 
        "thanks bye", 
        "have a great day",
        "talk to you later"
    }
};
```

### Context-Aware Assistant

```csharp
// Assistant that adapts based on candidate context
var contextAwareRequest = new VapiCallRequest
{
    PhoneNumber = candidatePhone,
    AssistantId = assistantId,
    AssistantOverrides = new VapiAssistantOverrides
    {
        VariableValues = new Dictionary<string, object>
        {
            { "candidateName", "John Doe" },
            { "jobTitle", "Senior Full Stack Developer" },
            { "experienceYears", 5 },
            { "currentCompany", "Tech Corp" },
            { "skillsRequired", "React, Node.js, AWS" },
            { "salaryRange", "80-100K USD" },
            { "workMode", "Hybrid" },
            { "location", "Bangalore" },
            { "interviewMode", "Technical Round" },
            { "previousInteractionDate", "2026-01-15" },
            { "applicationSource", "LinkedIn" }
        }
    }
};

// Then in system prompt, reference these variables:
content = @"You are calling {{candidateName}} who has {{experienceYears}} years of experience 
and is currently at {{currentCompany}}. They applied for the {{jobTitle}} position 
requiring skills in {{skillsRequired}}. The role is {{workMode}} based in {{location}}.

Tailor your conversation based on this context..."
```

---

## Multi-language Support

### Hindi Language Support

```csharp
var hindiAssistant = new
{
    name = "Hindi Interview Scheduler",
    transcriber = new
    {
        provider = "deepgram",
        model = "nova-2",
        language = "hi" // Hindi
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
                content = @"आप Risebird से एक पेशेवर भर्ती सहायक हैं जो इंटरव्यू शेड्यूल करने के लिए कॉल कर रहे हैं।

आपके उद्देश्य:
1. विनम्रता से अपना परिचय दें
2. उम्मीदवार की पहचान की पुष्टि करें
3. इंटरव्यू के अवसर के बारे में बताएं
4. उनकी उपलब्धता जांचें
5. एक उपयुक्त समय बुक करें

दिशानिर्देश:
- पेशेवर और दोस्ताना रहें
- स्पष्ट हिंदी बोलें
- उम्मीदवार की बात ध्यान से सुनें..."
            }
        }
    },
    voice = new
    {
        provider = "11labs",
        voiceId = "your_hindi_voice_id", // Get from ElevenLabs
        language = "hi"
    },
    firstMessage = "नमस्ते! मैं Risebird से बोल रही हूं। क्या मैं {{candidateName}} से बात कर सकती हूं?"
};
```

### Auto-detect Language

```csharp
transcriber = new
{
    provider = "deepgram",
    model = "nova-2",
    language = "multi" // Auto-detect language
},
model = new
{
    provider = "openai",
    model = "gpt-4o", // Supports 50+ languages
    messages = new[]
    {
        new
        {
            role = "system",
            content = @"You are a multilingual recruitment assistant. 
Detect the candidate's language and respond in the same language.
Supported languages: English, Hindi, Spanish, etc."
        }
    }
}
```

---

## Analytics Dashboard

### Get Call Statistics

```csharp
public async Task<IActionResult> GetAnalytics(DateTime? startDate, DateTime? endDate)
{
    var stats = await _vapiInterviewService.GetCallStatisticsAsync(startDate, endDate);
    
    var analytics = new
    {
        overview = new
        {
            totalCalls = stats.TotalCalls,
            successRate = $"{(stats.SuccessfulCalls * 100.0 / stats.TotalCalls):F1}%",
            bookingRate = $"{stats.BookingConversionRate:F1}%",
            totalCost = $"${stats.TotalCost:F2}",
            avgCostPerCall = $"${(stats.TotalCost / stats.TotalCalls):F2}"
        },
        breakdown = new
        {
            successful = stats.SuccessfulCalls,
            failed = stats.FailedCalls,
            booked = stats.BookingsCompleted,
            pendingCallback = stats.PendingCallbacks
        },
        performance = new
        {
            avgDuration = TimeSpan.FromSeconds(stats.AverageDurationSeconds).ToString(@"mm\:ss"),
            statusBreakdown = stats.CallsByStatus
        }
    };
    
    return Json(analytics);
}
```

### Real-time Dashboard View

```cshtml
@* In your dashboard view *@
<div class="row">
    <div class="col-md-3">
        <div class="card card-stats">
            <div class="card-body">
                <div class="row">
                    <div class="col-5">
                        <div class="icon-big text-center">
                            <i class="nc-icon nc-phone-2 text-primary"></i>
                        </div>
                    </div>
                    <div class="col-7">
                        <div class="numbers">
                            <p class="card-category">Total Calls</p>
                            <h4 class="card-title" id="totalCalls">0</h4>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-stats">
            <div class="card-body">
                <div class="row">
                    <div class="col-5">
                        <div class="icon-big text-center">
                            <i class="nc-icon nc-check-2 text-success"></i>
                        </div>
                    </div>
                    <div class="col-7">
                        <div class="numbers">
                            <p class="card-category">Interviews Booked</p>
                            <h4 class="card-title" id="bookedCount">0</h4>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-stats">
            <div class="card-body">
                <div class="row">
                    <div class="col-5">
                        <div class="icon-big text-center">
                            <i class="nc-icon nc-chart-bar-32 text-warning"></i>
                        </div>
                    </div>
                    <div class="col-7">
                        <div class="numbers">
                            <p class="card-category">Conversion Rate</p>
                            <h4 class="card-title" id="conversionRate">0%</h4>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-stats">
            <div class="card-body">
                <div class="row">
                    <div class="col-5">
                        <div class="icon-big text-center">
                            <i class="nc-icon nc-money-coins text-info"></i>
                        </div>
                    </div>
                    <div class="col-7">
                        <div class="numbers">
                            <p class="card-category">Total Cost</p>
                            <h4 class="card-title" id="totalCost">$0.00</h4>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
    function loadAnalytics() {
        $.get('/VapiInterviewCall/GetAnalytics', function(data) {
            $('#totalCalls').text(data.overview.totalCalls);
            $('#bookedCount').text(data.breakdown.booked);
            $('#conversionRate').text(data.overview.bookingRate);
            $('#totalCost').text(data.overview.totalCost);
        });
    }
    
    $(document).ready(function() {
        loadAnalytics();
        setInterval(loadAnalytics, 30000); // Refresh every 30 seconds
    });
</script>
```

---

## Interview Reminder System

### Automated Reminder Calls

```csharp
public async Task SendInterviewRemindersAsync()
{
    // Get interviews scheduled for tomorrow
    var tomorrow = DateTime.Today.AddDays(1);
    var upcomingInterviews = await _interviewRepository
        .GetInterviewsByDateAsync(tomorrow);
    
    foreach (var interview in upcomingInterviews)
    {
        var reminderAssistant = new VapiAssistant
        {
            Name = "Interview Reminder Assistant",
            Model = new VapiModel
            {
                Provider = "openai",
                Model = "gpt-4o-mini",
                Messages = new List<VapiMessage>
                {
                    new VapiMessage
                    {
                        Role = "system",
                        Content = @"You are calling to remind a candidate about their interview tomorrow.

Keep it brief:
1. Introduce yourself
2. Remind them about the interview (date, time, position)
3. Ask if they need any information
4. Confirm they'll attend
5. Wish them good luck

If they need to reschedule, offer to transfer to scheduling team."
                    }
                }
            },
            FirstMessage = "Hi {{candidateName}}! This is a friendly reminder from Risebird about your interview tomorrow at {{interviewTime}} for the {{jobTitle}} position."
        };
        
        var callRequest = new VapiCallRequest
        {
            PhoneNumber = interview.CandidatePhone,
            Assistant = reminderAssistant,
            AssistantOverrides = new VapiAssistantOverrides
            {
                VariableValues = new Dictionary<string, object>
                {
                    { "candidateName", interview.CandidateName },
                    { "interviewTime", interview.InterviewDate.ToString("h:mm tt") },
                    { "jobTitle", interview.JobTitle }
                }
            }
        };
        
        await _vapiService.InitiateCallAsync(callRequest);
    }
}
```

---

## Error Handling & Retry Logic

### Implement Retry for Failed Calls

```csharp
public async Task<bool> RetryFailedCallAsync(string callId, int maxRetries = 3)
{
    var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
    if (callLog == null) return false;
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            // Wait before retry (exponential backoff)
            if (attempt > 1)
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Pow(2, attempt - 1)));
            }
            
            var retryRequest = new VapiCallRequest
            {
                PhoneNumber = callLog.CandidatePhone,
                AssistantId = callLog.AssistantId,
                AssistantOverrides = new VapiAssistantOverrides
                {
                    VariableValues = new Dictionary<string, object>
                    {
                        { "candidateName", callLog.CandidateName },
                        { "isRetry", true },
                        { "attemptNumber", attempt }
                    }
                }
            };
            
            var response = await _vapiService.InitiateCallAsync(retryRequest);
            
            // If successful, update the log
            callLog.VapiCallId = response.Id;
            callLog.CallStatus = response.Status;
            callLog.UpdatedAt = DateTime.UtcNow;
            await _vapiCallRepository.UpdateCallLogAsync(callLog);
            
            return true;
        }
        catch (Exception ex)
        {
            await _logger.LogMessage(LogLevelInfo.Warning, 
                new Exception($"Retry attempt {attempt} failed for call {callId}: {ex.Message}"));
            
            if (attempt == maxRetries)
            {
                // All retries exhausted, log final failure
                await _logger.LogMessage(LogLevelInfo.Error, 
                    new Exception($"All {maxRetries} retry attempts failed for call {callId}"));
                return false;
            }
        }
    }
    
    return false;
}
```

---

## Integration with Existing Interview Flow

### Add to Existing Interview Scheduling

```csharp
// In your existing interview creation flow
public async Task<IActionResult> CreateInterview(InterviewViewModel model)
{
    // Your existing interview creation logic
    var interview = await _interviewService.CreateInterviewAsync(model);
    
    // NEW: Automatically initiate AI call for interview booking
    if (model.UseVoiceAIBooking)
    {
        try
        {
            var callRequest = new VapiCallRequest
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
            };
            
            var callResponse = await _vapiService.InitiateCallAsync(callRequest);
            
            // Log the call
            await _vapiCallRepository.SaveCallLogAsync(new VapiCallLog
            {
                VapiCallId = callResponse.Id,
                InterviewId = interview.InterviewId,
                CandidateId = model.CandidateId,
                CandidatePhone = model.CandidatePhone,
                CandidateName = model.CandidateName,
                CallStatus = callResponse.Status,
                CreatedAt = DateTime.UtcNow
            });
            
            ViewBag.Message = "Interview created and AI call initiated";
        }
        catch (Exception ex)
        {
            // Don't fail the entire operation if call fails
            await _logger.LogMessage(LogLevelInfo.Warning, ex);
            ViewBag.Message = "Interview created but AI call failed. Will retry later.";
        }
    }
    
    return View("InterviewCreated", interview);
}
```

---

## Advanced Webhook Handling

### Comprehensive Webhook Handler

```csharp
[HttpPost]
[AllowAnonymous]
public async Task<IActionResult> Webhook([FromBody] VapiWebhookMessage message)
{
    try
    {
        var messageType = message.Message.Type;
        
        switch (messageType)
        {
            case "assistant.started":
                await HandleAssistantStarted(message);
                return Ok();
                
            case "status-update":
                await HandleStatusUpdate(message);
                return Ok();
                
            case "transcript":
                await HandleTranscript(message);
                return Ok();
                
            case "tool-calls":
                var result = await HandleToolCalls(message);
                return Json(result); // Must return tool results
                
            case "end-of-call-report":
                await HandleEndOfCallReport(message);
                return Ok();
                
            case "speech-update":
                await HandleSpeechUpdate(message);
                return Ok();
                
            case "hang":
                await HandleHangUp(message);
                return Ok();
                
            default:
                await _logger.LogMessage(LogLevelInfo.Information,
                    new Exception($"Unhandled webhook type: {messageType}"));
                return Ok();
        }
    }
    catch (Exception ex)
    {
        await _logger.LogMessage(LogLevelInfo.Exception, ex);
        return StatusCode(500);
    }
}

private async Task<VapiToolCallResult> HandleToolCalls(VapiWebhookMessage message)
{
    var results = new List<VapiToolCallResultItem>();
    
    foreach (var toolCall in message.Message.ToolCalls.ToolCalls)
    {
        string result;
        
        switch (toolCall.Function.Name)
        {
            case "check_availability":
                result = await CheckRealAvailability(toolCall.Function.Arguments, message.Message.Call);
                break;
                
            case "book_interview":
                result = await BookRealInterview(toolCall.Function.Arguments, message.Message.Call);
                break;
                
            case "send_confirmation":
                result = await SendConfirmationEmail(toolCall.Function.Arguments, message.Message.Call);
                break;
                
            case "transfer_to_human":
                result = await InitiateHumanTransfer(message.Message.Call);
                break;
                
            default:
                result = JsonConvert.SerializeObject(new { 
                    success = false, 
                    message = "Unknown function" 
                });
                break;
        }
        
        results.Add(new VapiToolCallResultItem
        {
            ToolCallId = toolCall.Id,
            Result = result
        });
    }
    
    return new VapiToolCallResult { Results = results };
}
```

---

## Testing & Debugging

### Local Testing Setup

```bash
# 1. Install ngrok
choco install ngrok

# 2. Start your application
dotnet run --project Risebird.Web

# 3. Create tunnel
ngrok http https://localhost:44360

# 4. Update Vapi webhook URL to ngrok URL
# Example: https://abc123.ngrok.io/VapiInterviewCall/Webhook
```

### Mock Call Testing

```csharp
// Create a test endpoint that simulates Vapi responses
[HttpPost]
public async Task<IActionResult> TestCall()
{
    var mockWebhookMessage = new VapiWebhookMessage
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
                        Id = "call_test_001",
                        Type = "function",
                        Function = new VapiToolFunction
                        {
                            Name = "check_availability",
                            Arguments = new Dictionary<string, object>
                            {
                                { "preferredDate", "2026-02-05" },
                                { "preferredTime", "14:00" }
                            }
                        }
                    }
                }
            }
        }
    };
    
    var result = await _vapiService.ProcessToolCallAsync(mockWebhookMessage);
    return Json(result);
}
```

---

## Production Checklist

- [ ] Vapi API key configured in production
- [ ] Assistant created and ID saved
- [ ] Database table created
- [ ] Webhook URL is HTTPS and publicly accessible
- [ ] Service registrations added to DI container
- [ ] Calendar integration implemented
- [ ] Email service integrated for confirmations
- [ ] Error logging configured
- [ ] Call recording storage configured
- [ ] Privacy/HIPAA compliance reviewed
- [ ] Rate limiting configured
- [ ] Monitoring/alerts set up
- [ ] Cost tracking dashboard created
- [ ] Team trained on dashboard usage

---

## Performance Optimization

### Caching Assistant Configuration

```csharp
// Cache assistant ID to avoid repeated API calls
private static string _cachedAssistantId;
private static DateTime _cacheExpiry;

private async Task<string> GetOrCreateAssistantAsync()
{
    if (!string.IsNullOrEmpty(_cachedAssistantId) && DateTime.UtcNow < _cacheExpiry)
    {
        return _cachedAssistantId;
    }
    
    _cachedAssistantId = await _vapiService.CreateInterviewBookingAssistantAsync(
        "Interview Booking Assistant",
        webhookUrl);
    _cacheExpiry = DateTime.UtcNow.AddHours(24);
    
    return _cachedAssistantId;
}
```

### Async Call Processing

```csharp
// Process calls asynchronously without blocking
public async Task<IActionResult> InitiateBulkCallsAsync(List<int> candidateIds)
{
    // Queue the calls for background processing
    _ = Task.Run(async () =>
    {
        foreach (var candidateId in candidateIds)
        {
            try
            {
                await InitiateCallForCandidateAsync(candidateId);
                await Task.Delay(3000); // Rate limiting
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    });
    
    return Json(new { 
        success = true, 
        message = $"Queued {candidateIds.Count} calls for processing" 
    });
}
```

---

## Additional Resources

### Vapi API Endpoints Used

1. **POST** `/call/phone` - Initiate outbound call
2. **GET** `/call/{id}` - Get call details
3. **POST** `/assistant` - Create assistant
4. **GET** `/assistant` - List assistants
5. **DELETE** `/call/{id}` - Delete call (for privacy)

### Webhook Events Reference

| Event | Description | Response Required |
|-------|-------------|-------------------|
| `status-update` | Call status changed | No |
| `transcript` | Real-time transcript | No |
| `tool-calls` | Function execution needed | Yes |
| `end-of-call-report` | Call ended with full report | No |
| `assistant.started` | Assistant began speaking | No |
| `hang` | Call was hung up | No |

### Best Practices

1. **Always validate phone numbers** before calling
2. **Use variable values** for personalization
3. **Keep conversations under 5 minutes** for cost efficiency
4. **Monitor webhook failures** and implement retries
5. **Store transcripts** for compliance and quality assurance
6. **Analyze call patterns** to improve assistant prompts
7. **A/B test** different voices and prompts
8. **Set appropriate timeouts** for better user experience

