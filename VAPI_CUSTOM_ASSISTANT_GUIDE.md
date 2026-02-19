# Vapi Custom Interview Scheduling Assistant - Implementation Guide

## Overview
This guide explains how to use your custom Vapi assistant that collects candidate interview availability within 1-2 days and sends structured data back to your application.

## 1. Assistant Instructions (Updated Version)

### Key Improvements Made:
1. ✅ **Clear data extraction format** - Specifies YYYY-MM-DD for dates, HH:MM AM/PM for times
2. ✅ **Tool call integration** - Instructions to call `submitInterviewSlot` function
3. ✅ **Explicit confirmation requirement** - Won't submit without candidate saying "yes"
4. ✅ **Timezone handling** - All times in IST with conversion logic
5. ✅ **Error handling** - Clear instructions for edge cases
6. ✅ **Validation rules** - Checks dates are within 2 days before submission

### Suggested Tweaks to Your Original Instructions:

#### ❌ Remove: "Confirm country code and phone number"
**Why**: You already have their phone number. This wastes time and confuses candidates.

#### ✅ Add: "You MUST call the submitInterviewSlot function"
**Why**: Without this, the assistant won't send data to your webhook.

#### ✅ Add: Specific data format requirements
**Why**: Ensures consistent parsing of dates/times.

#### ✅ Add: "Verify candidate explicitly said 'yes' or confirmed"
**Why**: Prevents accidental bookings from vague responses.

#### ✅ Add: Handling for timezone mentions
**Why**: Some candidates might mention "6 PM my time" - need conversion logic.

## 2. Vapi Dashboard Configuration

### Step 1: Create the Tool Definition

In your Vapi assistant settings, add this tool under "Functions & Tools":

```json
{
  "type": "function",
  "function": {
    "name": "submitInterviewSlot",
    "description": "Submits the confirmed interview time slot to the booking system. ONLY call this after candidate explicitly confirms the date and time.",
    "parameters": {
      "type": "object",
      "properties": {
        "candidateName": {
          "type": "string",
          "description": "Full name of the candidate"
        },
        "interviewDate": {
          "type": "string",
          "description": "Date of interview in YYYY-MM-DD format (e.g., 2026-01-30)"
        },
        "interviewTime": {
          "type": "string",
          "description": "Time of interview in HH:MM AM/PM format (e.g., 02:30 PM)"
        },
        "timezone": {
          "type": "string",
          "description": "Timezone - should always be IST",
          "enum": ["IST"]
        },
        "candidateConfirmed": {
          "type": "boolean",
          "description": "Whether candidate explicitly confirmed the slot with 'yes' or similar affirmation"
        }
      },
      "required": ["candidateName", "interviewDate", "interviewTime", "timezone", "candidateConfirmed"]
    }
  },
  "server": {
    "url": "https://your-domain.com/VapiInterviewCall/Webhook",
    "timeoutSeconds": 20
  }
}
```

### Step 2: Configure Your Assistant

1. Go to Vapi Dashboard → Assistants
2. Create or edit your assistant
3. Paste the improved instructions (provided above) in the "System Prompt" section
4. Add the `submitInterviewSlot` tool/function
5. Set the webhook URL to your application: `https://your-domain.com/VapiInterviewCall/Webhook`
6. **Copy the Assistant ID** - You'll need this

### Step 3: Update appsettings.json

```json
{
  "Vapi": {
    "ApiKey": "your-vapi-api-key",
    "BaseUrl": "https://api.vapi.ai",
    "PhoneNumberId": "your-phone-number-id",
    "InterviewBookingAssistantId": "YOUR_CUSTOM_ASSISTANT_ID_HERE",
    "WebhookSecret": "optional-webhook-secret"
  },
  "HostDetails": {
    "baseUrl": "https://your-domain.com"
  }
}
```

## 3. Code Usage

### Making a Call with Your Custom Assistant

```csharp
// Example 1: From a controller action
[HttpPost]
public async Task<IActionResult> ScheduleInterviewCall(
    int interviewId,
    int candidateId, 
    string candidateName,
    string candidateEmail,
    string candidatePhone)
{
    try
    {
        // Ensure phone is in E.164 format
        if (!candidatePhone.StartsWith("+"))
        {
            candidatePhone = "+91" + candidatePhone;
        }

        // Create call request with your custom assistant
        var callRequest = new VapiCallRequest
        {
            PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
            Customer = new VapiCustomer
            {
                Number = candidatePhone,
                Name = candidateName,
                Email = candidateEmail
            },
            AssistantId = _configuration["Vapi:InterviewBookingAssistantId"] // Your custom assistant
        };

        // Initiate the call
        var callResponse = await _vapiService.InitiateCallAsync(callRequest);

        // Save to database
        var callLog = new VapiCallLog
        {
            VapiCallId = callResponse.Id,
            InterviewId = interviewId,
            RecipientId = candidateId,
            RecipientPhone = candidatePhone,
            RecipientName = candidateName,
            RecipientEmail = candidateEmail,
            CallStatus = callResponse.Status,
            AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "System"
        };

        await _vapiCallRepository.SaveCallLogAsync(callLog);

        return Json(new 
        { 
            success = true, 
            callId = callResponse.Id,
            message = "Interview scheduling call initiated"
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}
```

### Example 2: Bulk Calls for Multiple Candidates

```csharp
[HttpPost]
public async Task<IActionResult> ScheduleMultipleCalls([FromBody] List<RecipientCallInfo> candidates)
{
    var results = new List<object>();

    foreach (var candidate in candidates)
    {
        try
        {
            // Add delay to avoid rate limiting
            if (results.Count > 0)
            {
                await Task.Delay(2000); // 2 seconds between calls
            }

            var callRequest = new VapiCallRequest
            {
                PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                Customer = new VapiCustomer
                {
                    Number = candidate.RecipientPhone.StartsWith("+") 
                        ? candidate.RecipientPhone 
                        : "+91" + candidate.RecipientPhone,
                    Name = candidate.RecipientName,
                    Email = candidate.RecipientEmail
                },
                AssistantId = _configuration["Vapi:InterviewBookingAssistantId"]
            };

            var callResponse = await _vapiService.InitiateCallAsync(callRequest);

            var callLog = new VapiCallLog
            {
                VapiCallId = callResponse.Id,
                InterviewId = candidate.InterviewId,
                RecipientId = candidate.RecipientId,
                RecipientPhone = callRequest.Customer.Number,
                RecipientName = candidate.RecipientName,
                RecipientEmail = candidate.RecipientEmail,
                CallStatus = callResponse.Status,
                AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            await _vapiCallRepository.SaveCallLogAsync(callLog);

            results.Add(new 
            { 
                success = true, 
                candidateName = candidate.RecipientName,
                callId = callResponse.Id 
            });
        }
        catch (Exception ex)
        {
            results.Add(new 
            { 
                success = false, 
                candidateName = candidate.RecipientName,
                error = ex.Message 
            });
        }
    }

    return Json(new { results });
}
```

## 4. Webhook Processing Flow

### What Happens When a Call is Made:

1. **Call Initiated** → Your app calls Vapi API with assistant ID
2. **Vapi Calls Candidate** → Assistant follows your instructions
3. **Assistant Collects Slot** → Gets date, time, confirmation from candidate
4. **Assistant Calls Tool** → Executes `submitInterviewSlot` function
5. **Webhook Triggered** → Vapi sends tool call data to your webhook
6. **Your App Processes** → `HandleInterviewSlotSubmission` method updates database
7. **Call Ends** → Final end-of-call-report webhook received

### Webhook Events You'll Receive:

```
1. status-update (ringing, in-progress)
2. transcript (real-time conversation text)
3. tool-calls (submitInterviewSlot with booking data) ← MAIN EVENT
4. end-of-call-report (full transcript, recording, summary)
```

### Data Flow Diagram:

```
Candidate's Phone
       ↓
   Vapi Assistant (Collects: "Tomorrow 3 PM IST")
       ↓
   submitInterviewSlot({
       candidateName: "John Doe",
       interviewDate: "2026-01-30",
       interviewTime: "03:00 PM",
       timezone: "IST",
       candidateConfirmed: true
   })
       ↓
   Webhook → VapiInterviewCallController
       ↓
   HandleInterviewSlotSubmission()
       ↓
   Database Updated:
   - InterviewBooked = true
   - BookedInterviewDateTime = 2026-01-30 15:00:00
   - BookingConfirmationDetails = JSON with full details
```

## 5. Webhook Response Handling

The webhook at `/VapiInterviewCall/Webhook` automatically handles:

✅ **Validates data format** (date, time, confirmation status)
✅ **Parses interview date/time** to DateTime object
✅ **Checks date is within 2 days** from now
✅ **Updates VapiCallLogs table** with booking information
✅ **Logs events** for monitoring
✅ **Sends response back to Vapi** so assistant can continue conversation

### Database Fields Updated:

- `InterviewBooked` = `true`
- `BookedInterviewDateTime` = Parsed DateTime (e.g., 2026-01-30 15:00:00)
- `BookingConfirmationDetails` = JSON with:
  ```json
  {
    "candidateName": "John Doe",
    "interviewDate": "2026-01-30",
    "interviewTime": "03:00 PM",
    "timezone": "IST",
    "confirmedAt": "2026-01-29T10:30:00Z",
    "toolCallId": "abc123"
  }
  ```

## 6. Testing Your Setup

### Test Case 1: Successful Booking

**Call Flow:**
```
Assistant: "Hello, am I speaking with John Doe?"
Candidate: "Yes"
Assistant: "I'm calling from Risebird on behalf of SONATA regarding an interview opportunity. This is urgent hiring and we need to schedule within next 1-2 days. What date and time works for you?"
Candidate: "Tomorrow at 3 PM"
Assistant: "Let me confirm - Your SONATA interview is scheduled for January 30th at 3:00 PM IST. Is that correct?"
Candidate: "Yes, confirmed"
Assistant: [Calls submitInterviewSlot]
Assistant: "Perfect! You'll receive a confirmation email shortly."
```

**Expected Result:**
- ✅ Tool call webhook received with correct data
- ✅ Database updated with booking
- ✅ Logs show successful booking

### Test Case 2: Date Beyond 2 Days

**Call Flow:**
```
Assistant: "What date works for you?"
Candidate: "Next Monday"
Assistant: "This is urgent hiring. Only slots within next 1-2 days are available."
Candidate: "I'm only available next week"
Assistant: "I understand, but we can only offer slots in the next 1-2 days. If you're not available, I'll have to close this opportunity."
```

**Expected Result:**
- ❌ No tool call made
- ❌ No database update
- ✅ Call ends without booking

### Test Case 3: Candidate Not Interested

**Call Flow:**
```
Assistant: "Hello, am I speaking with Jane Smith?"
Candidate: "Yes, but I'm not interested"
Assistant: "I understand. Thank you for your time. Have a great day!"
[Call ends]
```

**Expected Result:**
- ❌ No tool call made
- ✅ Call status updated to "ended"
- ✅ EndedReason logged

## 7. Monitoring & Debugging

### View Call Logs

```csharp
// Get all calls for an interview
var calls = await _vapiCallRepository.GetCallLogsByInterviewIdAsync(interviewId);

// Get recent calls
var recentCalls = await _vapiCallRepository.GetRecentCallLogsAsync(50);

// Get specific call
var callDetails = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
```

### Check Booking Status

```csharp
// Check if interview was booked
if (callLog.InterviewBooked == true && callLog.BookedInterviewDateTime.HasValue)
{
    Console.WriteLine($"Interview booked for: {callLog.BookedInterviewDateTime.Value}");
    
    // Parse confirmation details
    var details = JsonConvert.DeserializeObject<dynamic>(callLog.BookingConfirmationDetails);
    Console.WriteLine($"Confirmed by: {details.candidateName}");
}
```

### Webhook Logs Location

Check your application error logs for:
- `"Interview slot booked via Vapi"` - Successful bookings
- `"Unhandled Vapi webhook message type"` - Unknown events
- Any exceptions during webhook processing

## 8. Common Issues & Solutions

### Issue 1: Tool Call Not Triggered

**Symptoms:** Call completes but no booking data received

**Possible Causes:**
- Assistant instructions don't mention calling the function
- Candidate didn't explicitly confirm
- Date was beyond 2 days

**Solution:**
- Ensure instructions say "You MUST call the submitInterviewSlot function"
- Check call transcript in Vapi dashboard
- Verify function definition is correct

### Issue 2: Invalid Date Format

**Symptoms:** Webhook receives data but parsing fails

**Solution:**
- Check the format in `BookingConfirmationDetails`
- Verify assistant is using YYYY-MM-DD format
- Update parsing formats in code if needed

### Issue 3: Webhook Not Receiving Data

**Symptoms:** Calls work but no webhooks arrive

**Possible Causes:**
- Webhook URL not configured in assistant
- Firewall blocking Vapi IPs
- Endpoint requires authentication

**Solution:**
- Verify webhook URL in Vapi dashboard
- Ensure `/VapiInterviewCall/Webhook` has `[AllowAnonymous]` attribute
- Check server logs for incoming requests

## 9. Next Steps

1. ✅ Update your assistant instructions in Vapi dashboard
2. ✅ Add the `submitInterviewSlot` tool definition
3. ✅ Copy your assistant ID to `appsettings.json`
4. ✅ Test with a real phone call
5. ✅ Monitor webhooks and database updates
6. ✅ Integrate with your email notification system
7. ✅ Add interview to your calendar system when booking confirmed

## 10. Production Checklist

- [ ] Webhook URL is HTTPS (not HTTP)
- [ ] Assistant ID configured in appsettings
- [ ] Phone number ID configured
- [ ] Webhook secret configured (optional but recommended)
- [ ] Error logging is working
- [ ] Database backups are enabled
- [ ] Tested with multiple scenarios
- [ ] Email confirmations are being sent
- [ ] Calendar integrations working
- [ ] Monitored first 10-20 calls in production

---

**Need Help?** Check the Vapi documentation at https://docs.vapi.ai or review the webhook logs in your application.
