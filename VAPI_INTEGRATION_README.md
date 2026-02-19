# Vapi.ai Interview Booking Integration

## Overview
This integration enables automated voice AI calls to candidates for interview booking using [Vapi.ai](https://vapi.ai/).

## Features
- 🤖 **AI Voice Agent**: Natural conversation with candidates
- 📞 **Outbound Calls**: Automatically call candidates for interview scheduling
- 📅 **Smart Booking**: Check availability and book interview slots
- 📝 **Call Transcripts**: Full conversation transcripts saved
- 📊 **Analytics**: Call summaries and structured data extraction
- 🔗 **Webhook Integration**: Real-time updates on call status

## Architecture

### Components Created

#### 1. **Models** (`Risebird.Entities.Data.VapiModels/`)
- `VapiCallRequest.cs` - Request models for initiating calls
- `VapiCallResponse.cs` - Response models from Vapi API
- `VapiWebhookMessage.cs` - Webhook message models
- `VapiCallLog.cs` - Database entity for call logs

#### 2. **Service Layer**
- `IVapiService.cs` - Service interface
- `VapiService.cs` - Vapi API integration implementation

#### 3. **Repository Layer**
- `IVapiCallRepository.cs` - Repository interface
- `VapiCallRepository.cs` - Database operations for call logs

#### 4. **Controller**
- `VapiInterviewCallController.cs` - API endpoints for call management

#### 5. **Database**
- `VapiCallLogs.sql` - Table schema for storing call data

#### 6. **View**
- `Index.cshtml` - Dashboard for managing calls

## Setup Instructions

### Step 1: Get Vapi API Key
1. Sign up at [vapi.ai](https://vapi.ai/)
2. Navigate to Dashboard → Settings → API Keys
3. Create a new API key

### Step 2: Configure appsettings.json
Add the following to your `appsettings.json`:

```json
{
  "Vapi": {
    "ApiKey": "YOUR_VAPI_API_KEY_HERE",
    "BaseUrl": "https://api.vapi.ai",
    "InterviewBookingAssistantId": ""
  }
}
```

### Step 3: Create Database Table
Run the SQL script:

```bash
# Execute the SQL file
sqlcmd -S your_server -d your_database -i Risebird.DB\Tables\VapiCallLogs.sql
```

Or manually execute the contents of `VapiCallLogs.sql` in SQL Server Management Studio.

### Step 4: Register Services in Startup/Program.cs

Add to your service registration:

```csharp
// In Program.cs or Startup.cs
services.AddHttpClient();
services.AddScoped<IVapiService, VapiService>();
services.AddScoped<IVapiCallRepository, VapiCallRepository>();
```

### Step 5: Setup Vapi Assistant
1. Navigate to `/VapiInterviewCall` in your application
2. Click "Setup Assistant" button
3. Copy the Assistant ID returned
4. Update `appsettings.json` with the Assistant ID:
   ```json
   "Vapi": {
     "InterviewBookingAssistantId": "asst_xxxxx..."
   }
   ```

### Step 6: Configure Webhooks
Ensure your webhook URL is publicly accessible:
- URL format: `https://your-domain.com/VapiInterviewCall/Webhook`
- Vapi will send POST requests to this endpoint
- If testing locally, use ngrok or similar tunneling service

## Usage

### Making a Call

#### Via UI:
1. Go to `/VapiInterviewCall`
2. Click "Initiate New Call"
3. Fill in candidate details:
   - Interview ID
   - Candidate ID
   - Name, Email, Phone
   - Job Title
   - Preferred date/time (optional)
4. Click "Start Call"

#### Via API:
```csharp
var callRequest = new VapiCallRequest
{
    PhoneNumber = "+919876543210",
    AssistantId = "asst_xxxxx...",
    AssistantOverrides = new VapiAssistantOverrides
    {
        VariableValues = new Dictionary<string, object>
        {
            { "candidateName", "John Doe" },
            { "jobTitle", "Senior Developer" },
            { "interviewId", 123 },
            { "candidateId", 456 }
        }
    }
};

var response = await _vapiService.InitiateCallAsync(callRequest);
```

### Call Flow

1. **Call Initiation**: System calls candidate's phone number
2. **Introduction**: AI introduces itself as Risebird recruiter
3. **Identity Verification**: Confirms speaking with correct candidate
4. **Purpose**: Explains the interview opportunity
5. **Availability Check**: 
   - Asks for candidate's availability
   - Calls your `check_availability` function
   - Provides available slots
6. **Booking**:
   - Candidate selects a time
   - Calls your `book_interview` function
   - Confirms booking details
7. **Closing**: Thanks candidate and ends call

### Webhook Events

Vapi sends webhooks for various events:

#### `status-update`
Called when call status changes (queued → ringing → in-progress → ended)

#### `transcript`
Real-time transcript updates as conversation progresses

#### `tool-calls`
When AI needs to execute a function (check availability, book interview)

**Expected Response Format:**
```json
{
  "results": [
    {
      "toolCallId": "call_xxxxx",
      "result": "{\"success\": true, \"availableSlots\": [...]}"
    }
  ]
}
```

#### `end-of-call-report`
Comprehensive report at call end with:
- Full transcript
- Recording URL
- Call duration
- Cost breakdown
- Analysis summary
- Structured data

## Customization

### Modifying the Assistant Prompt

Edit `VapiService.cs` → `CreateInterviewBookingAssistantAsync()` → System message:

```csharp
content = @"Your custom system prompt here...
Adjust tone, conversation flow, etc."
```

### Changing Voice

Available voice providers:
- **11Labs** (ElevenLabs) - Premium voices
- **Deepgram** - Fast and affordable
- **OpenAI** - Natural sounding
- **Cartesia** - Low latency

Example configuration:
```csharp
voice = new
{
    provider = "11labs",
    voiceId = "burt", // Male voice options: burt, phillip, steve, joseph
    // Female: sarah, melissa, andrea, marissa
    stability = 0.5,
    similarityBoost = 0.75,
    speed = 1.0
}
```

### Adding Custom Tools

Add to the `tools` array in `CreateInterviewBookingAssistantAsync()`:

```csharp
new
{
    type = "function",
    function = new
    {
        name = "send_confirmation_email",
        description = "Send interview confirmation email to candidate",
        parameters = new
        {
            type = "object",
            properties = new
            {
                email = new { type = "string", description = "Candidate email" },
                interviewDetails = new { type = "string", description = "Interview details" }
            }
        }
    },
    server = new { url = webhookUrl }
}
```

### Integrating with Your Calendar System

Update the `CheckAvailability()` method in `VapiService.cs`:

```csharp
private string CheckAvailability(Dictionary<string, object> arguments)
{
    // Get interview ID from metadata
    var interviewId = // extract from call context
    
    // Query your database for available slots
    var slots = await _yourCalendarService.GetAvailableSlotsAsync(interviewId);
    
    return JsonConvert.SerializeObject(new { 
        success = true, 
        availableSlots = slots 
    });
}
```

Update the `BookInterview()` method:

```csharp
private string BookInterview(Dictionary<string, object> arguments)
{
    var interviewDateTime = arguments["interviewDateTime"].ToString();
    var interviewId = // extract from metadata
    
    // Save to your database
    await _yourBookingService.BookInterviewAsync(interviewId, interviewDateTime);
    
    // Send confirmation email
    await _emailService.SendConfirmationAsync(...);
    
    return JsonConvert.SerializeObject(new { 
        success = true, 
        bookingId = "..." 
    });
}
```

## API Endpoints

### POST `/VapiInterviewCall/InitiateInterviewCall`
Initiates a call to a candidate

**Request Body:**
```json
{
  "interviewId": 123,
  "candidateId": 456,
  "candidateName": "John Doe",
  "candidateEmail": "john@example.com",
  "candidatePhone": "+919876543210",
  "jobTitle": "Senior Developer",
  "preferredDateTime": "2026-02-01T10:00:00"
}
```

**Response:**
```json
{
  "success": true,
  "callId": "call_xxxxx",
  "logId": 1,
  "status": "queued",
  "message": "Call initiated successfully"
}
```

### GET `/VapiInterviewCall/GetCallStatus?callId=call_xxxxx`
Gets current call status and details

### GET `/VapiInterviewCall/GetInterviewCalls?interviewId=123`
Lists all calls for an interview

### POST `/VapiInterviewCall/Webhook`
Webhook endpoint for Vapi callbacks (don't call directly)

## Cost Considerations

Typical costs per call (approximate):
- **Transcription** (Deepgram): $0.0055/min
- **LLM** (GPT-4o-mini): $0.003/min
- **Voice** (ElevenLabs): $0.06/min
- **Transport** (Phone): $0.01/min

**Total**: ~$0.08/minute or ~$0.40 for a 5-minute call

### Cost Optimization:
1. Use GPT-4o-mini instead of GPT-4o
2. Use Deepgram for transcription (cheaper than alternatives)
3. Set `maxDurationSeconds` to limit call length
4. Use `numFastTurns` for initial turns with faster models

## Advanced Features

### Voicemail Detection
Automatically detect and leave voicemail:

```csharp
voicemailDetection = new
{
    provider = "twilio",
    enabled = true,
    machineDetectionTimeout = 30
},
voicemailMessage = "Hi, this is Sarah from Risebird. Please call us back..."
```

### Multi-language Support
```csharp
transcriber = new
{
    provider = "deepgram",
    model = "nova-2",
    language = "hi" // Hindi, or "es" for Spanish, etc.
},
voice = new
{
    provider = "11labs",
    voiceId = "your_multilingual_voice_id",
    language = "hi"
}
```

### Call Recording
Recording is enabled by default. Access via:
```csharp
var recording = callLog.RecordingUrl;
// Download or stream the recording
```

### Structured Data Extraction
Extract specific data from conversations:

```csharp
artifactPlan = new
{
    structuredDataPlan = new
    {
        enabled = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                candidateInterest = new { type = "string", enum = new[] { "interested", "not_interested", "maybe" } },
                availabilityWeek = new { type = "string" },
                preferredTime = new { type = "string" }
            }
        }
    }
}
```

## Monitoring & Analytics

### Call Metrics
- Success rate
- Average duration
- Booking conversion rate
- Common end reasons

### Query Examples:
```sql
-- Successful bookings
SELECT COUNT(*) FROM VapiCallLogs WHERE InterviewBooked = 1;

-- Average call duration
SELECT AVG(DurationSeconds) FROM VapiCallLogs WHERE CallStatus = 'ended';

-- Calls by status
SELECT CallStatus, COUNT(*) as Count 
FROM VapiCallLogs 
GROUP BY CallStatus;
```

## Troubleshooting

### Call Not Connecting
1. Verify phone number is in E.164 format (+[country][number])
2. Check Vapi account has sufficient credits
3. Verify webhook URL is publicly accessible

### Assistant Not Responding
1. Check OpenAI API key in Vapi dashboard
2. Verify assistant ID is correct
3. Review assistant configuration

### Webhooks Not Received
1. Ensure webhook URL is HTTPS
2. Check firewall/security rules
3. Use Vapi dashboard to test webhook delivery

### Tool Calls Failing
1. Verify webhook returns proper JSON format
2. Check server timeout settings
3. Review function parameter schemas

## Security Best Practices

1. **API Key**: Store in environment variables or Azure Key Vault
2. **Webhook Authentication**: Validate webhook signatures (implement HMAC)
3. **Data Privacy**: 
   - Enable HIPAA mode if handling sensitive data
   - Set `hipaaEnabled: true` in assistant config
4. **Call Recordings**: Auto-delete after retention period
5. **PCI Compliance**: If collecting payment info, enable PCI mode

## Testing

### Local Testing with ngrok:
```bash
# Install ngrok
ngrok http https://localhost:44360

# Use the ngrok URL as webhook
# Example: https://abc123.ngrok.io/VapiInterviewCall/Webhook
```

### Test Call Script:
```javascript
// Use Vapi dashboard's "Test Call" feature
// Or call the API directly
POST /VapiInterviewCall/InitiateInterviewCall
{
  "interviewId": 1,
  "candidateId": 1,
  "candidateName": "Test Candidate",
  "candidateEmail": "test@example.com",
  "candidatePhone": "+919999999999",
  "jobTitle": "Developer"
}
```

## Resources

- [Vapi Documentation](https://docs.vapi.ai/)
- [Vapi Dashboard](https://dashboard.vapi.ai/)
- [API Reference](https://docs.vapi.ai/api-reference)
- [Community Support](https://discord.gg/vapi)

## Example Conversation Flow

```
🤖 AI: "Hello! This is Sarah from Risebird. May I speak with John Doe?"

👤 Candidate: "Yes, this is John."

🤖 AI: "Hi John! I'm calling about the Senior Developer position. We'd like to schedule an interview with you. Do you have a moment to discuss your availability?"

👤 Candidate: "Sure, I'm available next week."

🤖 AI: "Great! Let me check our available slots for next week."
     [Calls check_availability tool]
     "We have openings on Tuesday at 10 AM, 2 PM, and Thursday at 4 PM. Which works best for you?"

👤 Candidate: "Tuesday at 2 PM works for me."

🤖 AI: "Perfect! Let me confirm - Tuesday, February 4th at 2 PM. Is that correct?"

👤 Candidate: "Yes, that's correct."

🤖 AI: [Calls book_interview tool]
     "Excellent! Your interview has been booked for Tuesday, February 4th at 2 PM. You'll receive a confirmation email shortly with all the details. Do you have any questions?"

👤 Candidate: "No, that's all. Thank you!"

🤖 AI: "You're welcome! We look forward to speaking with you. Have a great day!"
     [Ends call]
```

## Cost Estimation

For 100 candidates:
- Average call duration: 3 minutes
- Cost per call: ~$0.24
- **Total: $24**

Success rate typically: 60-70% (bookings/attempted calls)

## Next Steps

1. ✅ Setup Vapi account and get API key
2. ✅ Configure appsettings.json
3. ✅ Run database migration
4. ✅ Setup assistant via UI
5. 🔄 Integrate with your calendar system (update `CheckAvailability` method)
6. 🔄 Integrate with booking system (update `BookInterview` method)
7. 🔄 Connect to your email service for confirmations
8. 🔄 Test with a few candidates
9. 🔄 Monitor and optimize based on analytics

## Support

For issues or questions:
- Check Vapi documentation: https://docs.vapi.ai/
- Review call logs in dashboard
- Check Application Insights for errors
- Contact Vapi support: support@vapi.ai
