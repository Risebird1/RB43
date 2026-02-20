# Quick Vapi Setup - 3 Minute Guide

## Your Webhook URL
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

---

## Step 1: Update Assistant Instructions (2 minutes)

### Go to Vapi Dashboard
1. Visit: https://vapi.ai
2. Click **"Assistants"** in sidebar
3. Select your SONATA assistant

### Paste Instructions
1. Find **"System Prompt"** section
2. Open file: `IMPROVED_ASSISTANT_INSTRUCTIONS.txt`
3. Copy **ALL** content (Ctrl+A, Ctrl+C)
4. Paste into System Prompt field
5. Click **"Save"**

✅ **Done with instructions!**

---

## Step 2: Add the Tool (1 minute)

### Add Tool to Assistant
1. In same assistant page, scroll to **"Tools"** section (or **"Functions"** on older UI)
2. Click **"Add Tool"** or **"Add Function"** button
3. Switch to **"JSON"** mode (if available)
4. Open file: `VAPI_TOOL_DEFINITION.json`
5. Copy **ALL** content
6. Paste into JSON field
7. Click **"Save"** or **"Add Tool"**

**The webhook URL is already set in the JSON to:**
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

✅ **Done with tool!**

---

## Step 3: Set Global Webhook (30 seconds)

### Configure Assistant Webhook
1. In same assistant page, find **"Server URL"** or **"Webhook URL"** field
2. Enter: `https://portal.risebird.io/VapiInterviewCall/Webhook`
3. Click **"Save"**

✅ **Done with webhook!**

---

## Step 4: Copy Assistant ID (30 seconds)

### Get Your Assistant ID
1. At top of assistant page, find **"Assistant ID"**
2. Copy the ID (looks like: `abc123-def456-...`)
3. Open: `Risebird.Web\appsettings.json`
4. Update this line:
   ```json
   "InterviewBookingAssistantId": "PASTE_YOUR_ID_HERE",
   ```
5. Save file

✅ **Setup complete!**

---

## Verification Checklist

Before testing, confirm:

- [ ] System prompt updated in Vapi ✓
- [ ] Tool `submitInterviewSlot` added ✓
- [ ] Webhook URL set in tool definition ✓
- [ ] Webhook URL set in assistant settings ✓
- [ ] Assistant ID copied to appsettings.json ✓

---

## Test Your Setup

```csharp
// Make a test call
var callRequest = new VapiCallRequest
{
    PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
    Customer = new VapiCustomer
    {
        Number = "+919876543210", // Your test number
        Name = "Test Candidate",
        Email = "test@example.com"
    },
    AssistantId = _configuration["Vapi:InterviewBookingAssistantId"]
};

var response = await _vapiService.InitiateCallAsync(callRequest);
```

---

## What to Expect in Test Call

**Assistant will:**
1. ✅ Greet candidate: "Hello, am I speaking with Test Candidate?"
2. ✅ State purpose: "I'm calling from Risebird on behalf of SONATA..."
3. ✅ **ASK for time:** "What date and time works for you - today or tomorrow?"
4. ✅ Wait for candidate to **tell them** a time (NOT suggest one)
5. ✅ Confirm: "Let me confirm - Your interview is [DATE] at [TIME] IST. Correct?"
6. ✅ After "yes", call the function and update database

**Assistant will NOT:**
- ❌ Suggest specific times like "How about 2 PM?"
- ❌ Confirm pre-set times
- ❌ Accept dates beyond 2 days

---

## Where Webhook Sends Data

```
Call Flow:
  Candidate → Vapi Assistant → Collects slot
                             ↓
                    submitInterviewSlot(...)
                             ↓
  https://portal.risebird.io/VapiInterviewCall/Webhook
                             ↓
              VapiInterviewCallController.HandleInterviewSlotSubmission()
                             ↓
                    VapiCallLogs table updated ✅
```

---

## Troubleshooting

**Problem:** Tool not called
- Check assistant says "You MUST call the submitInterviewSlot function"
- Verify candidate said "yes" or confirmed explicitly
- Ensure tool is properly saved in "Tools" section

**Problem:** Webhook not receiving data
- Verify URL is exactly: `https://portal.risebird.io/VapiInterviewCall/Webhook`
- Check application logs for errors
- Ensure endpoint has `[AllowAnonymous]` attribute

**Problem:** Assistant suggesting times instead of asking
- Review call transcript in Vapi dashboard
- Instructions now explicitly say "ASK, don't suggest times"
- The assistant should wait for candidate to propose a time

**Note:** Vapi is migrating from "Functions" to "Tools" - both work the same way!

---

## Key Points About Assistant Behavior

### ✅ CORRECT Behavior:
```
Assistant: "What time works for you?"
Candidate: "3 PM tomorrow"
Assistant: "Let me confirm - tomorrow at 3 PM IST. Correct?"
Candidate: "Yes"
Assistant: [Calls submitInterviewSlot]
```

### ❌ INCORRECT Behavior (Now Prevented):
```
Assistant: "How about 2 PM tomorrow?"  ← DON'T DO THIS
Candidate: "Okay"
Assistant: [Calls submitInterviewSlot]
```

**The updated instructions explicitly prevent the assistant from suggesting times.**

---

## Files Reference

- **Instructions:** `IMPROVED_ASSISTANT_INSTRUCTIONS.txt`
- **Tool JSON:** `VAPI_TOOL_DEFINITION.json`
- **Detailed Guide:** `VAPI_DASHBOARD_SETUP_GUIDE.md`
- **Code Examples:** `VAPI_CODE_EXAMPLES.cs`

---

**Setup Time:** ~3 minutes  
**Your webhook URL:** `https://portal.risebird.io/VapiInterviewCall/Webhook`  
**Ready to test!** 🚀
