# Webhook Issue Resolution - VapiCallLogs Not Saving

**Date:** February 20, 2026  
**Issue:** Data not saving to VapiCallLogs table, validation error about candidate name

---

## 🔍 Root Causes Identified

### 1. **Deserialization Bug** (CRITICAL)
The `VapiToolFunction.Arguments` property was incorrectly configured with `[JsonExtensionData]`, which failed to properly map the "arguments" field from the webhook JSON.

**Result:** The Arguments dictionary was empty/null, causing all parameter extractions to fail.

### 2. **Incorrect candidateName Value**
The system prompt instructed the AI to use a hardcoded `candidateName` template value (which was the email address "harshitjajoo95@gmail.com") instead of asking the candidate for their actual name.

**Result:** Tool calls submitted email addresses instead of actual names, triggering validation errors.

### 3. **Missing Validation**
No validation existed to prevent email addresses from being accepted as candidate names.

---

## ✅ Fixes Applied

### 1. Fixed Deserialization (`VapiWebhookMessage.cs`)
**File:** `Risebird.Entities\Data\VapiModels\VapiWebhookMessage.cs`

**Before:**
```csharp
[JsonExtensionData]
public Dictionary<string, object> Arguments { get; set; }
```

**After:**
```csharp
[JsonProperty("arguments")]
public Dictionary<string, object> Arguments { get; set; }
```

**Impact:** Arguments will now properly deserialize from the webhook JSON.

---

### 2. Updated System Prompt (`VapiInterviewCallController.cs`)
**File:** `Risebird.Web\Controllers\VapiInterviewCallController.cs`

**Changes:**
- Modified call flow step 1: Removed greeting with pre-filled name
- Added call flow step 2: "May I have your full name please?" to collect actual name
- Updated step 6: Instructs AI to use "The EXACT full name the candidate provided in step 2"
- Added validation rule: "Candidate name MUST be their actual full name, NOT an email address"
- Added NEVER rule: "Use pre-filled or template values for candidateName"

**Impact:** AI will now ask for and use the candidate's actual name from the conversation.

---

### 3. Added Email Validation (`VapiService.cs`)
**File:** `Risebird.CommonHelper\VapiService.cs`

**New validation:**
```csharp
// Validate that candidateName is not an email address
if (candidateName.Contains("@"))
{
    return JsonConvert.SerializeObject(new
    {
        success = false,
        message = "The candidate name appears to be an email address. Please ask for the candidate's actual full name (first and last name)."
    });
}
```

**Impact:** Email addresses will be rejected, prompting the AI to ask for the actual name.

---

### 4. Updated Tool Definitions
**Files:** `VAPI_TOOL_DEFINITION.json`, `VAPI_PARAMETERS_ONLY.json`

**Updated candidateName description:**
```json
"description": "Full name of the candidate as spoken/confirmed by them during the call. Must be their actual name (e.g., 'John Smith'), NOT their email address or phone number. Ask the candidate 'May I have your full name please?' and use their response."
```

**Impact:** Clear instructions for AI on how to collect the candidate name.

---

## 🔧 Required Next Steps

### 1. Rebuild the Application
```powershell
dotnet build
```

### 2. Update Vapi Assistant Configuration
You need to update the assistant configuration in the Vapi dashboard:

1. Go to https://dashboard.vapi.ai
2. Navigate to your assistant configuration
3. Update the `submitInterviewSlot` tool definition
4. Paste the updated content from `VAPI_TOOL_DEFINITION.json`
5. Save the changes

### 3. Test with a New Call
Initiate a new test call and verify:
- [ ] AI asks "May I have your full name please?"
- [ ] AI uses the actual name provided (not email)
- [ ] Tool call succeeds (no validation error)
- [ ] Data is saved to VapiCallLogs table
- [ ] WebhookDebugLogs shows ProcessingStatus = "success"

---

## 📊 Expected Behavior After Fix

### Correct Call Flow:
1. **Bot:** "Hello, this is Risebird Interviews calling. Is this a good time to speak?"
2. **User:** "Yes"
3. **Bot:** "I'm calling from Risebird on behalf of SONATA regarding an interview opportunity for .NET Developer. May I have your full name please?"
4. **User:** "Harsh Jajoo" (or actual name)
5. **Bot:** "This is urgent hiring and we need to schedule your interview within the next 1-2 days. Would you be available tomorrow or day after tomorrow?"
6. *(Continue with scheduling...)*

### Correct Tool Call:
```json
{
  "candidateName": "Harsh Jajoo",  // ✅ Actual name, not email
  "interviewDate": "2026-02-21",
  "interviewTime": "10:00 AM",
  "timezone": "IST",
  "candidateConfirmed": true
}
```

### Database Records:
- **VapiCallLogs:** New record created with proper candidate name
- **WebhookDebugLogs:** ProcessingStatus = "success", DataSaved = 1

---

## 🐛 Why Data Wasn't Saving Before

The issue was a **cascade failure**:

1. ❌ `Arguments` dictionary was empty due to deserialization bug
2. ❌ `candidateName` extracted as NULL from empty dictionary
3. ❌ Validation failed: "Please confirm the candidate's name before proceeding."
4. ❌ Even though `HandleInterviewSlotSubmission` should run, the Arguments being empty meant it couldn't extract proper data
5. ❌ The method likely returned early or threw an exception before reaching database save logic

---

## 📝 Additional Notes

### Why the Assistant Asked About Name Issue
The AI received the validation error message back from the tool call:
```json
{"success": false, "message": "Please confirm the candidate's name before proceeding."}
```

The AI interpreted this as needing to ask for the name again, which is why it ended the call mentioning there was an issue with the name.

### Testing the Fix
After rebuilding and updating the Vapi assistant:
1. Make a test call
2. Check WebhookDebugLogs for the tool-call with ProcessingStatus
3. Verify VapiCallLogs has a new entry
4. Verify the candidateName field contains an actual name, not an email

---

## 🎯 Summary
- **Main Issue:** Deserialization bug preventing Arguments from being populated
- **Secondary Issue:** System prompt using email instead of asking for actual name
- **Fix Status:** Code fixes applied ✅
- **Next Action:** Rebuild application and update Vapi assistant configuration
