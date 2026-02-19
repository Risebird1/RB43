# Vapi Tool Setup - Current UI Instructions

## ✅ For the CURRENT Vapi Interface (Form-Based)

Based on your error, Vapi now uses a **form-based UI** where you fill different sections separately.

---

## Step-by-Step Setup

### 1. Click "+ Add Tool" or "+ Add Function"

### 2. Fill Out These Fields:

#### **Name:**
```
submitInterviewSlot
```

#### **Description:**
```
Submits the confirmed interview time slot to the booking system. ONLY call this after candidate explicitly confirms the date and time with words like 'yes', 'confirmed', 'okay', or similar affirmation.
```

---

### 3. Parameters Section (JSON Schema)

**In the "Parameters" field/box, paste THIS (and ONLY this):**

Open the file: **`VAPI_PARAMETERS_ONLY.json`**

Copy and paste its entire contents into the Parameters field.

It should start with:
```json
{
  "type": "object",
  "properties": {
```

And end with:
```json
  ]
}
```

**DO NOT paste the outer `"function"` wrapper!** That was causing your error.

---

### 4. Server Configuration

Look for fields like:

#### **Server URL** or **Endpoint URL:**
```
https://portal.risebird.io/VapiInterviewCall/Webhook
```

#### **Timeout (seconds):**
```
20
```

---

### 5. Click Save/Add

---

## 🎯 What Was Wrong

### ❌ You Pasted (WRONG):
```json
{
  "type": "function",           ← This outer wrapper caused the error
  "function": {
    "name": "submitInterviewSlot",
    "parameters": {
      "type": "object",
      ...
    }
  },
  "server": {...}
}
```

### ✅ You Should Paste (RIGHT):
```json
{
  "type": "object",             ← Starts here!
  "properties": {
    "candidateName": {...},
    ...
  },
  "required": [...]
}
```

---

## 📋 Complete Form Fields Summary

| Field | Value |
|-------|-------|
| **Name** | `submitInterviewSlot` |
| **Description** | Submits the confirmed interview time slot to the booking system. ONLY call this after candidate explicitly confirms the date and time. |
| **Parameters** | Paste contents of `VAPI_PARAMETERS_ONLY.json` |
| **Server URL** | `https://portal.risebird.io/VapiInterviewCall/Webhook` |
| **Timeout** | `20` |

---

## 🔍 The Error Explained

Your error:
```
Invalid literal value, expected "object"
```

This happened because:
- You pasted `"type": "function"` 
- But the Parameters field expects `"type": "object"`

The Parameters field only wants the **schema for the parameters**, not the entire function definition!

---

## Files to Use

### For the Whole Tool (OLD):
- ❌ `VAPI_TOOL_DEFINITION.json` - Don't use this in current UI!

### For the Current UI (NEW):
- ✅ **Name field:** Type `submitInterviewSlot`
- ✅ **Description field:** Copy from instructions
- ✅ **Parameters field:** Paste `VAPI_PARAMETERS_ONLY.json`
- ✅ **Server URL field:** Type `https://portal.risebird.io/VapiInterviewCall/Webhook`

---

## Visual Guide

```
Vapi Tool Form
┌────────────────────────────────────────────┐
│                                            │
│ Name: submitInterviewSlot                  │
│       ^^^^^^^^^^^^^^^^^^^^^^               │
│       Type this                            │
│                                            │
│ Description:                               │
│ ┌────────────────────────────────────────┐│
│ │ Submits the confirmed interview...     ││
│ │ ^^^^^^^^^^^^^^^^^^^^^^^^^              ││
│ │ Copy from instructions                 ││
│ └────────────────────────────────────────┘│
│                                            │
│ Parameters: (JSON Schema)                  │
│ ┌────────────────────────────────────────┐│
│ │ {                                      ││
│ │   "type": "object",                    ││
│ │   "properties": {                      ││
│ │     ...                                ││
│ │   }                                    ││
│ │ }                                      ││
│ │ ^^^^^^^^^^^^^^^^^^^^^^^^^              ││
│ │ Paste VAPI_PARAMETERS_ONLY.json here   ││
│ └────────────────────────────────────────┘│
│                                            │
│ Server URL:                                │
│ https://portal.risebird.io/VapiInterview...│
│ ^^^^^^^^^^^^^^^^^^^^^^^^^^                 │
│ Type this                                  │
│                                            │
│ Timeout: 20                                │
│                                            │
│ [Save]  [Cancel]                           │
└────────────────────────────────────────────┘
```

---

## ✅ Quick Checklist

- [ ] Name: `submitInterviewSlot`
- [ ] Description: Copy from instructions text
- [ ] Parameters: Paste `VAPI_PARAMETERS_ONLY.json` (NOT the full tool definition!)
- [ ] Server URL: `https://portal.risebird.io/VapiInterviewCall/Webhook`
- [ ] Timeout: `20`
- [ ] Click Save

---

## Still Getting Errors?

If you still get JSON schema errors:

1. **Make sure** you're pasting from `VAPI_PARAMETERS_ONLY.json`
2. **NOT** from `VAPI_TOOL_DEFINITION.json`
3. The JSON should start with `{"type": "object"` - not `{"type": "function"`

---

**The key difference:** Vapi's form wants you to fill each section separately, not paste one big JSON!
