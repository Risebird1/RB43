# WHERE TO PASTE THE VAPI TOOL JSON - EXACT LOCATION

## 🚨 IMPORTANT: You're Probably in Form Mode!

If you see a "**Parameters section**", you're in **Form Mode**, not **JSON Mode**.

You need to **switch to JSON mode** first!

---

## Step-by-Step: Finding the Right Place

### Step 1: Open Your Assistant
```
Vapi Dashboard → Assistants → [Your SONATA Assistant]
```

### Step 2: Find the Tools/Functions Section

Scroll down until you see:
```
┌─────────────────────────────────────┐
│ Tools                    [+ Add]    │  ← Click this button
│                                     │
│ (or "Functions" on older UI)        │
└─────────────────────────────────────┘
```

### Step 3: Click the Add Button

Click either:
- **[+ Add Tool]** (new UI)
- **[+ Add Function]** (old UI)

A popup/modal will appear.

---

## Step 4: YOU'LL SEE THIS FIRST (Form Mode):

```
┌──────────────────────────────────────────────────┐
│  Add Tool                                  [×]   │
├──────────────────────────────────────────────────┤
│                                                  │
│  ○ Form Mode    ○ JSON Mode  ← LOOK HERE!      │
│     ^^^^^^^                                      │
│     YOU ARE HERE (wrong place)                   │
│                                                  │
│  Tool Name: ____________________                 │
│                                                  │
│  Description:                                    │
│  ┌──────────────────────────────────────────┐   │
│  │                                          │   │
│  └──────────────────────────────────────────┘   │
│                                                  │
│  Parameters:                     [+ Add]         │
│  ┌──────────────────────────────────────────┐   │
│  │ (empty)                                  │   │  ← DON'T PASTE HERE!
│  └──────────────────────────────────────────┘   │
│                                                  │
│  [Add]  [Cancel]                                 │
└──────────────────────────────────────────────────┘
```

**❌ DON'T fill this form!**  
**❌ DON'T paste JSON in "Parameters" section!**

---

## Step 5: SWITCH TO JSON MODE

Look at the top of the popup. You'll see:

```
○ Form Mode    ○ JSON Mode
     ^             ^
   (current)    (click this!)
```

**Click on "JSON Mode"** (the right circle)

---

## Step 6: NOW YOU'LL SEE THIS (JSON Mode):

```
┌──────────────────────────────────────────────────┐
│  Add Tool                                  [×]   │
├──────────────────────────────────────────────────┤
│                                                  │
│  ○ Form Mode    ● JSON Mode  ← NOW HERE!       │
│                   ^^^^^^^                        │
│                   SELECTED                       │
│                                                  │
│  ┌──────────────────────────────────────────┐   │
│  │ {                                        │   │
│  │                                          │   │
│  │                                          │   │  ← PASTE HERE!
│  │                                          │   │
│  │                                          │   │
│  │                                          │   │
│  │ }                                        │   │
│  └──────────────────────────────────────────┘   │
│                                                  │
│  [Add]  [Cancel]                                 │
└──────────────────────────────────────────────────┘
```

**✅ THIS IS WHERE YOU PASTE THE JSON!**

---

## Step 7: Paste the Entire JSON

1. Open file: `VAPI_TOOL_DEFINITION.json`
2. Select **ALL** content (Ctrl+A)
3. Copy (Ctrl+C)
4. Click inside the big text box in JSON Mode
5. Paste (Ctrl+V)

You should see:

```
┌──────────────────────────────────────────────────┐
│  Add Tool                                  [×]   │
├──────────────────────────────────────────────────┤
│                                                  │
│  ○ Form Mode    ● JSON Mode                     │
│                                                  │
│  ┌──────────────────────────────────────────┐   │
│  │ {                                        │   │
│  │   "type": "function",                    │   │
│  │   "function": {                          │   │
│  │     "name": "submitInterviewSlot",       │   │
│  │     "description": "Submits the...",     │   │
│  │     "parameters": {                      │   │
│  │       "type": "object",                  │   │
│  │       "properties": {                    │   │
│  │         "candidateName": {               │   │
│  │           "type": "string",              │   │
│  │           "description": "Full name..."  │   │
│  │         },                               │   │
│  │         "interviewDate": { ... },        │   │
│  │         "interviewTime": { ... },        │   │
│  │         "timezone": { ... },             │   │
│  │         "candidateConfirmed": { ... }    │   │
│  │       },                                 │   │
│  │       "required": [...]                  │   │
│  │     }                                    │   │
│  │   },                                     │   │
│  │   "server": {                            │   │
│  │     "url": "https://portal.risebird..." │   │
│  │     "timeoutSeconds": 20                 │   │
│  │   }                                      │   │
│  │ }                                        │   │
│  └──────────────────────────────────────────┘   │
│                                                  │
│  [Add]  [Cancel]                                 │
└──────────────────────────────────────────────────┘
```

---

## Step 8: Click Add/Save

Click the **[Add]** or **[Save]** button at the bottom.

---

## What If I Don't See "JSON Mode"?

If you only see Form Mode (no JSON option):

### Option A: Check for a Toggle
Look for:
- A toggle switch at the top
- A dropdown menu
- A "Switch to JSON" button

### Option B: Fill the Form Manually (Last Resort)

If JSON mode truly doesn't exist, fill the form like this:

```
Tool Name: submitInterviewSlot

Description: 
Submits the confirmed interview time slot to the booking system. 
ONLY call this after candidate explicitly confirms the date and time.

Parameters (click [+ Add] for each):

1. Name: candidateName
   Type: string
   Description: Full name of the candidate
   Required: ✓

2. Name: interviewDate  
   Type: string
   Description: Date in YYYY-MM-DD format (e.g., 2026-01-30)
   Required: ✓

3. Name: interviewTime
   Type: string
   Description: Time in HH:MM AM/PM format (e.g., 02:30 PM)
   Required: ✓

4. Name: timezone
   Type: string
   Description: Always IST
   Required: ✓

5. Name: candidateConfirmed
   Type: boolean
   Description: True only if candidate said "yes", "confirmed", etc.
   Required: ✓

Server URL: https://portal.risebird.io/VapiInterviewCall/Webhook

Timeout: 20
```

---

## Visual Summary

```
WRONG ❌                          RIGHT ✅
─────────                         ─────────
Form Mode                         JSON Mode
├─ Tool Name field                ├─ Big text box
├─ Description field              ├─ Paste entire JSON here
├─ Parameters section    →        └─ All config in one place
│  └─ DON'T PASTE HERE!
└─ Server URL field
```

---

## Common Mistakes

### ❌ WRONG: Pasting JSON in Parameters Section
```
Parameters: [+ Add]
┌──────────────────────────┐
│ { "type": "function"...  │  ← NO! This won't work!
└──────────────────────────┘
```

### ✅ RIGHT: Paste in JSON Mode Text Box
```
○ Form Mode    ● JSON Mode
┌──────────────────────────┐
│ {                        │
│   "type": "function",    │  ← YES! Entire JSON here!
│   "function": {          │
│   ...                    │
│ }                        │
└──────────────────────────┘
```

---

## Quick Checklist

- [ ] Click **[+ Add Tool]** button
- [ ] Look for **○ Form Mode    ○ JSON Mode** at top
- [ ] Click on **JSON Mode** (right circle)
- [ ] See a **big empty text box** appear
- [ ] Paste **entire JSON** from `VAPI_TOOL_DEFINITION.json`
- [ ] Click **[Add]** button
- [ ] Done! ✓

---

## Still Confused?

### What you're seeing now:
"I see a Parameters section with an [+ Add] button"

### What you should do:
1. Look at the **very top** of the popup
2. Find "**Form Mode**" and "**JSON Mode**"
3. Click "**JSON Mode**"
4. The Parameters section will **disappear**
5. A big text box will **appear**
6. Paste the JSON in that text box

---

## Screenshot Reference

**BEFORE (Form Mode - DON'T USE):**
```
╔══════════════════════════════════════╗
║ Add Tool                        [×]  ║
╠══════════════════════════════════════╣
║ ○ Form Mode    ○ JSON Mode          ║  ← Click JSON Mode!
║                                      ║
║ Tool Name: [____________]            ║
║                                      ║
║ Description:                         ║
║ [_________________________]          ║
║                                      ║
║ Parameters:             [+ Add]      ║  ← Ignore this
║ [_________________________]          ║
║                                      ║
║ [Add]  [Cancel]                      ║
╚══════════════════════════════════════╝
```

**AFTER (JSON Mode - USE THIS):**
```
╔══════════════════════════════════════╗
║ Add Tool                        [×]  ║
╠══════════════════════════════════════╣
║ ○ Form Mode    ● JSON Mode          ║  ← JSON Mode selected!
║                                      ║
║ ┌────────────────────────────────┐  ║
║ │ {                              │  ║
║ │   "type": "function",          │  ║  ← Paste entire JSON here
║ │   "function": {                │  ║
║ │     "name": "submitInterview..." │ ║
║ │     ...entire JSON content...  │  ║
║ │   }                            │  ║
║ │ }                              │  ║
║ └────────────────────────────────┘  ║
║                                      ║
║ [Add]  [Cancel]                      ║
╚══════════════════════════════════════╝
```

---

## Key Point

**You paste the ENTIRE JSON file content in ONE place:**
- The big text box that appears in **JSON Mode**
- NOT in individual Form Mode fields
- NOT in the Parameters section

The JSON contains everything: name, description, parameters, server URL - all in one!

---

**Bottom Line:** Switch to JSON Mode first, then paste the entire JSON!
