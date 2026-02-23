# Vapi: Tools vs Functions - What You Need to Know

## 🔄 Vapi is Migrating Terminology

Vapi is transitioning from "Functions" to "Tools" terminology. **Both work identically** - it's just a naming change!

---

## What This Means for You

### Old UI (Functions)
```
Assistants → Your Assistant → Functions → Add Function
```

### New UI (Tools)
```
Assistants → Your Assistant → Tools → Add Tool
```

**Everything else is THE SAME!**

---

## Which UI Will You See?

### You'll see "Tools" if:
- ✅ You have the latest Vapi dashboard version
- ✅ You recently created your account
- ✅ Vapi has rolled out the update to your account

### You'll see "Functions" if:
- ⚠️ You have an older dashboard version
- ⚠️ Vapi hasn't migrated your account yet
- ⚠️ You're using a legacy assistant

**Don't worry! Both work exactly the same way.**

---

## What Stays the Same

No matter which UI you see:

✅ **JSON structure is identical**
- Still uses `"type": "function"`
- Still has `"function"` object
- Still has `"server"` configuration

✅ **Configuration is identical**
- Same webhook URL
- Same parameters
- Same timeout settings

✅ **Behavior is identical**
- Assistant calls tools the same way
- Webhook receives same data
- Your code works unchanged

---

## Updated Guides

All guides now mention **both** "Tools" and "Functions" terminology:

### When You See "Tools" in Guides:
- It refers to the NEW UI
- Look for "Tools" section in your dashboard

### When You See "Functions" in Guides:
- It refers to the OLD UI
- Look for "Functions" section in your dashboard

### When You See "Tools/Functions" in Guides:
- Works for BOTH UIs
- Use whichever you see

---

## Step-by-Step: Adding a Tool/Function

### Step 1: Navigate to Your Assistant
```
Vapi Dashboard → Assistants → [Your Assistant]
```

### Step 2: Find the Section
Look for either:
- **"Tools"** (new UI) ← Most likely this
- **"Functions"** (old UI)

### Step 3: Click Add Button
Click either:
- **"Add Tool"** button
- **"Add Function"** button

### Step 4: Paste JSON
1. Switch to **"JSON"** mode
2. Paste content from `VAPI_TOOL_DEFINITION.json`
3. Click **"Save"** or **"Add"**

### Step 5: Verify
You should see:
- **Tool name:** `submitInterviewSlot`
- **Parameters:** 5 properties
- **Server URL:** Your webhook

---

## Common Questions

### Q: I see "Functions" but the guide says "Tools". Is this wrong?
**A:** No! You're on the older UI. Everything works the same. Just read "Functions" wherever the guide says "Tools".

### Q: Will my existing functions stop working?
**A:** No! If you already configured functions, they continue working perfectly. No action needed.

### Q: Should I recreate my function as a tool?
**A:** No need! Your existing function works fine. Only create new if you're starting fresh.

### Q: The JSON says "type": "function" but you call it a "tool"?
**A:** Yes, the JSON structure still uses `"function"` internally. This is normal and correct. Vapi just renamed the UI section from "Functions" to "Tools".

---

## Visual Comparison

### OLD UI (Functions)
```
┌──────────────────────────────────────────┐
│ Assistant Configuration                   │
├──────────────────────────────────────────┤
│ Name: SONATA Scheduler                   │
│ System Prompt: [...]                     │
│ Server URL: [...]                        │
│                                          │
│ ┌─ FUNCTIONS ─────────────────────────┐ │
│ │                         [Add +]      │ │
│ │ 📋 submitInterviewSlot    [Edit] [×]│ │
│ └──────────────────────────────────────┘ │
└──────────────────────────────────────────┘
```

### NEW UI (Tools)
```
┌──────────────────────────────────────────┐
│ Assistant Configuration                   │
├──────────────────────────────────────────┤
│ Name: SONATA Scheduler                   │
│ System Prompt: [...]                     │
│ Server URL: [...]                        │
│                                          │
│ ┌─ TOOLS ────────────────────────────┐  │
│ │                         [Add +]     │  │
│ │ 📋 submitInterviewSlot    [Edit] [×]│  │
│ └─────────────────────────────────────┘  │
└──────────────────────────────────────────┘
```

**Same functionality, just different label!**

---

## JSON Structure (Unchanged)

Both UIs use this exact same JSON:

```json
{
  "type": "function",          ← Still says "function"!
  "function": {
    "name": "submitInterviewSlot",
    "description": "...",
    "parameters": { ... }
  },
  "server": {
    "url": "https://portal.risebird.io/VapiInterviewCall/Webhook",
    "timeoutSeconds": 20
  }
}
```

**This works for both "Tools" and "Functions" sections!**

---

## Quick Reference

| What You See | What To Do |
|-------------|-----------|
| "Tools" section | Click "Add Tool" |
| "Functions" section | Click "Add Function" |
| "Add Tool" button | Click it and paste JSON |
| "Add Function" button | Click it and paste JSON |

**End result is the same!**

---

## Your Setup Still Works

Your existing configuration with webhook URL:
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

Still works perfectly whether it's under:
- ✅ "Tools" section
- ✅ "Functions" section
- ✅ Assistant server configuration

---

## Bottom Line

**Don't overthink it!** 

- If you see "Tools" → use "Tools"
- If you see "Functions" → use "Functions"
- Either way → paste the same JSON
- Either way → use the same webhook URL
- Either way → your code works!

The migration is just cosmetic UI terminology. The functionality is 100% identical.

---

## When in Doubt

Follow these guides - they now support BOTH terminologies:

- **`QUICK_VAPI_SETUP.md`** - 3-minute setup (updated)
- **`VAPI_DASHBOARD_SETUP_GUIDE.md`** - Detailed guide (updated)
- **`VAPI_DASHBOARD_VISUAL_GUIDE.txt`** - Visual layouts (updated)

All guides now say "Tools/Functions" or mention both options!

---

**No action required if your setup already works!** 🎉
