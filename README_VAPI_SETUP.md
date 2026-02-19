# Vapi Interview Scheduling Setup - README

## 🚨 Important Notice: "Functions" → "Tools"

Vapi is transitioning from "**Functions**" to "**Tools**" terminology.

**Don't worry!** Both work identically - it's just a UI naming change.

---

## Which One Will You See?

When you open your Vapi assistant, you'll see either:

### Option 1: "Tools" (New UI) ✨
```
Assistant → Tools → Add Tool
```

### Option 2: "Functions" (Old UI) 
```
Assistant → Functions → Add Function
```

**Both work exactly the same!** Just use whichever you see.

---

## Quick Setup Guide

### Step 1: Update Instructions
1. Go to your assistant in Vapi dashboard
2. Paste content from: **`IMPROVED_ASSISTANT_INSTRUCTIONS.txt`**
3. Save

### Step 2: Add the Tool
1. Find "**Tools**" or "**Functions**" section
2. Click "**Add Tool**" or "**Add Function**"
3. Switch to JSON mode
4. Paste content from: **`VAPI_TOOL_DEFINITION.json`**
5. Save

### Step 3: Set Webhook URLs
Set this URL in **TWO places**:
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

**Place 1:** Assistant → Server URL  
**Place 2:** Tool definition → server.url (already in JSON)

### Step 4: Update Config
1. Copy your Assistant ID from Vapi
2. Update `appsettings.json`:
   ```json
   "InterviewBookingAssistantId": "YOUR_ASSISTANT_ID"
   ```

---

## 📚 Documentation Files

### Quick Start (Pick One):
- **`QUICK_VAPI_SETUP.md`** - 3-minute guide (UPDATED ✅)
- **`VAPI_DASHBOARD_VISUAL_GUIDE.txt`** - Visual diagrams (UPDATED ✅)

### Detailed Guides:
- **`VAPI_DASHBOARD_SETUP_GUIDE.md`** - Step-by-step (UPDATED ✅)
- **`VAPI_CUSTOM_ASSISTANT_GUIDE.md`** - Full implementation guide
- **`VAPI_TOOLS_VS_FUNCTIONS.md`** - Explains the terminology change (NEW ✨)

### Reference:
- **`VAPI_CODE_EXAMPLES.cs`** - Code examples
- **`IMPROVED_ASSISTANT_INSTRUCTIONS.txt`** - Paste into Vapi
- **`VAPI_TOOL_DEFINITION.json`** - Paste into Vapi

---

## ✅ All Guides Are Updated

Every guide now supports BOTH "Tools" and "Functions" terminology:

- ✅ Visual guides show both options
- ✅ Step-by-step instructions mention both
- ✅ Screenshots reference both UIs
- ✅ No code changes needed

---

## Key Points

### Your Webhook URL:
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

### Tool/Function Name:
```
submitInterviewSlot
```

### What It Does:
- ✅ Asks candidate for available time (doesn't suggest)
- ✅ Collects date and time from candidate
- ✅ Requires explicit confirmation ("yes")
- ✅ Only accepts slots within 1-2 days
- ✅ Sends structured JSON to your webhook
- ✅ Auto-updates database

---

## Terminology Changes

| Old Name | New Name | Status |
|----------|----------|--------|
| Functions | Tools | Both work! |
| Add Function | Add Tool | Both work! |
| Function definition | Tool definition | Both work! |

**The JSON structure is unchanged - still uses `"type": "function"`**

---

## Need Help?

### If you see "Tools":
- Follow any guide and read "Tools" wherever it says "Functions/Tools"
- Everything works the same

### If you see "Functions":
- Follow any guide and read "Functions" wherever it says "Tools/Functions"
- Everything works the same

### If you're confused:
- Read: **`VAPI_TOOLS_VS_FUNCTIONS.md`**
- It explains everything in detail

---

## Testing Your Setup

```csharp
var callRequest = new VapiCallRequest
{
    PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
    Customer = new VapiCustomer
    {
        Number = "+919876543210",
        Name = "Test Candidate",
        Email = "test@example.com"
    },
    AssistantId = _configuration["Vapi:InterviewBookingAssistantId"]
};

var response = await _vapiService.InitiateCallAsync(callRequest);
```

---

## Summary

✅ All guides updated for Tools/Functions  
✅ Both UIs are fully supported  
✅ No code changes required  
✅ Webhook URL stays the same  
✅ JSON structure unchanged  
✅ Your setup will work with either UI  

**Ready to go!** 🚀

---

**Start here:** `QUICK_VAPI_SETUP.md` for fastest setup!
