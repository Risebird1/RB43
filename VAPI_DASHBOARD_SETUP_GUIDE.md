# Vapi Dashboard Setup Guide - Step-by-Step

## Your Webhook URL
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

---

## Part 1: Configuring Your Assistant in Vapi Dashboard

### Step 1: Log in to Vapi Dashboard
1. Go to https://vapi.ai
2. Log in to your account
3. Navigate to **"Assistants"** in the left sidebar

### Step 2: Select Your Assistant
1. Find your assistant (the one you created for SONATA interviews)
2. Click on it to open the configuration page

---

## Part 2: Adding the System Prompt (Instructions)

### Step 3: Update System Prompt
1. In your assistant configuration, look for **"System Prompt"** or **"Instructions"** section
2. Clear any existing text
3. Copy the ENTIRE content from: `IMPROVED_ASSISTANT_INSTRUCTIONS.txt`
4. Paste it into the System Prompt field
5. Click **"Save"** or **"Update"**

**Screenshot Reference:**
```
┌─────────────────────────────────────────────────┐
│ Assistant Configuration                          │
├─────────────────────────────────────────────────┤
│                                                  │
│ Name: SONATA Interview Scheduler                │
│                                                  │
│ System Prompt: ↓                                │
│ ┌─────────────────────────────────────────────┐ │
│ │ You are an outbound interview scheduling   │ │
│ │ assistant from Risebird Interviews...       │ │
│ │                                             │ │
│ │ [PASTE YOUR FULL INSTRUCTIONS HERE]        │ │
│ │                                             │ │
│ └─────────────────────────────────────────────┘ │
│                                                  │
│         [Save]  [Cancel]                         │
└─────────────────────────────────────────────────┘
```

---

## Part 3: Adding the Tool Definition

### Step 4: Locate the Tools Section
1. In the same assistant configuration page, scroll down to find:
   - **"Tools"** section (new UI), OR
   - **"Functions"** section (older UI)
2. Click **"Add Tool"** or **"Add Function"** button

**Note:** Vapi is transitioning from "Functions" terminology to "Tools". Both work identically - just different naming!

### Step 5: Add the Tool Using JSON

**Option A: JSON Mode (Recommended)**
1. If there's a toggle between "Form" and "JSON", select **"JSON"**
2. Copy the ENTIRE content from: `VAPI_TOOL_DEFINITION.json`
3. Paste it into the JSON field
4. **IMPORTANT:** The webhook URL is already set to:
   ```json
   "server": {
     "url": "https://portal.risebird.io/VapiInterviewCall/Webhook",
     "timeoutSeconds": 20
   }
   ```
5. Click **"Save"** or **"Add Tool"**

**Screenshot Reference:**
```
┌─────────────────────────────────────────────────┐
│ Tools                                [Add +]     │
├─────────────────────────────────────────────────┤
│                                                  │
│ ○ Form Mode    ● JSON Mode                     │
│                                                  │
│ ┌─────────────────────────────────────────────┐ │
│ │ {                                           │ │
│ │   "type": "function",                       │ │
│ │   "function": {                             │ │
│ │     "name": "submitInterviewSlot",          │ │
│ │     "description": "Submits the confirmed..." │ │
│ │     ...                                     │ │
│ │   },                                        │ │
│ │   "server": {                               │ │
│ │     "url": "https://portal.risebird.io/...│ │
│ │     "timeoutSeconds": 20                    │ │
│ │   }                                         │ │
│ │ }                                           │ │
│ └─────────────────────────────────────────────┘ │
│                                                  │
│         [Add Tool]  [Cancel]                     │
└─────────────────────────────────────────────────┘
```

**Option B: Form Mode (If JSON not available)**
If Vapi only shows a form interface, fill it manually:

1. **Tool/Function Name:** `submitInterviewSlot`

2. **Description:** 
   ```
   Submits the confirmed interview time slot to the booking system. 
   ONLY call this after candidate explicitly confirms the date and time.
   ```

3. **Parameters:** Click "Add Parameter" or "Add Property" for each:

   | Parameter Name | Type | Description | Required |
   |---------------|------|-------------|----------|
   | `candidateName` | string | Full name of the candidate | Yes |
   | `interviewDate` | string | Date in YYYY-MM-DD format (e.g., 2026-01-30) | Yes |
   | `interviewTime` | string | Time in HH:MM AM/PM format (e.g., 02:30 PM) | Yes |
   | `timezone` | string | Always IST | Yes |
   | `candidateConfirmed` | boolean | True only if candidate said "yes", "confirmed", etc. | Yes |

4. **Server URL:** `https://portal.risebird.io/VapiInterviewCall/Webhook`

5. **Timeout:** `20` seconds

6. Click **"Save"** or **"Add Tool"**

---

## Part 4: Configuring the Global Webhook URL

### Step 6: Set Assistant-Level Webhook
1. In your assistant configuration, look for:
   - **"Server URL"** field, OR
   - **"Webhook URL"** field, OR
   - **"Server Configuration"** section
2. Enter: `https://portal.risebird.io/VapiInterviewCall/Webhook`
3. Click **"Save"**

**Screenshot Reference:**
```
┌─────────────────────────────────────────────────┐
│ Server Configuration                             │
├─────────────────────────────────────────────────┤
│                                                  │
│ Server URL: *                                    │
│ ┌─────────────────────────────────────────────┐ │
│ │ https://portal.risebird.io/VapiInterviewCa..│ │
│ └─────────────────────────────────────────────┘ │
│                                                  │
│ Timeout (seconds): 20                            │
│                                                  │
│         [Save]  [Cancel]                         │
└─────────────────────────────────────────────────┘
```

**Important Note:** You need to set the webhook URL in TWO places:
1. ✅ In the tool definition (Step 5) - under "server.url"
2. ✅ In the assistant's server configuration (Step 6) - under "Server URL"

Both should be: `https://portal.risebird.io/VapiInterviewCall/Webhook`

**Terminology Note:** Vapi uses "Tools" in the new UI and "Functions" in older UI. They work identically!

---

## Part 5: Copy Your Assistant ID

### Step 7: Get the Assistant ID
1. In your assistant configuration page, look for:
   - **"Assistant ID"** at the top, OR
   - **"ID"** field in the details section
2. It will look like: `5a7c4ecb-fcba-4fa6-bcf1-48842d742144`
3. **Copy this ID** - you'll need it for your configuration

**Screenshot Reference:**
```
┌─────────────────────────────────────────────────┐
│ SONATA Interview Scheduler                       │
├─────────────────────────────────────────────────┤
│ Assistant ID: 5a7c4ecb-fcba-4fa6-bcf1-48842d... │
│ [Copy ID]                                        │
└─────────────────────────────────────────────────┘
```

### Step 8: Update Your Application Configuration
1. Open: `Risebird.Web\appsettings.json`
2. Find the `"Vapi"` section
3. Update `"InterviewBookingAssistantId"` with your new assistant ID:
   ```json
   "Vapi": {
     "ApiKey": "63c0746a-9ed5-4241-81ea-37bb6d9bed32",
     "BaseUrl": "https://api.vapi.ai",
     "InterviewBookingAssistantId": "YOUR_NEW_ASSISTANT_ID_HERE",
     "PhoneNumberId": "e7de2354-2c12-4ff4-ad06-fc574a8502d3"
   }
   ```
4. Save the file

---

## Part 6: Verify Your Setup

### Verification Checklist

Before making a test call, verify:

- [ ] System prompt is updated in Vapi dashboard
- [ ] Tool `submitInterviewSlot` is added to assistant
- [ ] Tool webhook URL is: `https://portal.risebird.io/VapiInterviewCall/Webhook`
- [ ] Assistant server URL is: `https://portal.risebird.io/VapiInterviewCall/Webhook`
- [ ] Assistant ID is copied
- [ ] `appsettings.json` is updated with correct Assistant ID
- [ ] Application is restarted (if it was running)

---

## Common Issues & Solutions

### Issue 1: "Webhook URL not configured"
**Solution:** Make sure you set the webhook URL in BOTH:
1. The tool/function definition
2. The assistant's server configuration

### Issue 2: "Tool not found" or "Function not found"
**Solution:** 
- Check that tool/function name is exactly: `submitInterviewSlot` (case-sensitive)
- Verify the tool is saved in the assistant's "Tools" section
- Try refreshing the Vapi dashboard page
- Check if you're looking at "Tools" vs "Functions" - Vapi is transitioning terminology

### Issue 3: "Assistant not using the tool"
**Solution:**
- Ensure the system prompt says: "You MUST call the submitInterviewSlot function"
- Check that candidate confirmed with "yes" or similar
- Review the call transcript in Vapi dashboard to see what happened

### Issue 4: "Webhook not receiving data"
**Solution:**
- Verify the URL is exactly: `https://portal.risebird.io/VapiInterviewCall/Webhook`
- No trailing slash
- HTTPS (not HTTP)
- Check your application logs for incoming requests

---

## Visual Summary: Where Things Go

```
┌────────────────────────────────────────────────────────┐
│               VAPI DASHBOARD CONFIGURATION              │
├────────────────────────────────────────────────────────┤
│                                                         │
│  [1] ASSISTANT                                          │
│      ├─ Name: SONATA Interview Scheduler               │
│      ├─ Assistant ID: [Copy this]                      │
│      ├─ System Prompt: [Paste instructions here]       │
│      ├─ Server URL: https://portal.risebird.io/Vapi... │
│      └─ Functions ↓                                     │
│                                                         │
│  [2] FUNCTION: submitInterviewSlot                      │
│      ├─ Name: submitInterviewSlot                      │
│      ├─ Description: Submits confirmed slot...          │
│      ├─ Parameters:                                     │
│      │   ├─ candidateName (string)                     │
│      │   ├─ interviewDate (string)                     │
│      │   ├─ interviewTime (string)                     │
│      │   ├─ timezone (string)                          │
│      │   └─ candidateConfirmed (boolean)               │
│      └─ Server URL: https://portal.risebird.io/Vapi... │
│                                                         │
└────────────────────────────────────────────────────────┘

                          ↓ Calls to

┌────────────────────────────────────────────────────────┐
│           YOUR APPLICATION (Risebird.Web)               │
├────────────────────────────────────────────────────────┤
│                                                         │
│  Endpoint: /VapiInterviewCall/Webhook                   │
│  Method: POST                                           │
│  Handler: HandleInterviewSlotSubmission()               │
│  Updates: VapiCallLogs table                            │
│                                                         │
└────────────────────────────────────────────────────────┘
```

---

## Quick Reference Card

**Webhook URL (use everywhere):**
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

**Tool/Function Name:**
```
submitInterviewSlot
```

**Where to Set Webhook URL:**
1. ✅ Function definition → server.url
2. ✅ Assistant configuration → Server URL

**Required Parameters:**
- candidateName (string)
- interviewDate (string, YYYY-MM-DD)
- interviewTime (string, HH:MM AM/PM)
- timezone (string, "IST")
- candidateConfirmed (boolean)

---

## Next Steps After Configuration

1. **Test the assistant:**
   ```csharp
   // Make a test call to your own phone
   var callRequest = new VapiCallRequest
   {
       PhoneNumberId = "e7de2354-2c12-4ff4-ad06-fc574a8502d3",
       Customer = new VapiCustomer
       {
           Number = "+91YOUR_NUMBER",
           Name = "Test User",
           Email = "test@example.com"
       },
       AssistantId = "YOUR_NEW_ASSISTANT_ID"
   };
   
   var response = await _vapiService.InitiateCallAsync(callRequest);
   ```

2. **Monitor the webhook:**
   - Check application logs at: `C:\Users\harsh\source\repos\Risebird\logs\`
   - Look for: `"Interview slot booked via Vapi"`

3. **Check the database:**
   ```sql
   SELECT TOP 10 * 
   FROM VapiCallLogs 
   ORDER BY CreatedAt DESC
   ```

4. **Review call transcript in Vapi:**
   - Go to Vapi Dashboard → Calls
   - Find your test call
   - Click to see full transcript and events

---

**Questions?** Refer to `VAPI_CUSTOM_ASSISTANT_GUIDE.md` for detailed troubleshooting.
