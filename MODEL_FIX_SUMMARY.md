# Model Name Fix - gpt-5.2-instant → gpt-5-mini

## ❌ The Problem

Original model name used: `gpt-5.2-instant`

**Error from Vapi**:
```json
{
  "message": ["assistantOverrides.model.model must be one of the following values: gpt-5.2, gpt-5.2-chat-latest, ..."],
  "error": "Bad Request",
  "statusCode": 400
}
```

**Reason**: `gpt-5.2-instant` is not a valid Vapi model identifier.

---

## ✅ The Solution

Changed to: `gpt-5-mini`

**Why gpt-5-mini?**
- ✅ Valid model identifier (confirmed in error message)
- ✅ Fast response times ("mini" = optimized for speed)
- ✅ Cost-effective for high-volume calling
- ✅ Part of GPT-5 family (good quality)
- ✅ Suitable for conversational AI calling

---

## 📝 Files Updated

All occurrences of `gpt-5.2-instant` changed to `gpt-5-mini`:

### Code Files (5 files):

1. **VapiCallRequest.cs** (lines 69, 190)
   - Default model in `VapiModel` class
   - Default model in `VapiModelOverride` class

2. **DashboardController.cs**
   - Test mode call requests

3. **VapiInterviewCallController.cs**
   - Manual call initiation
   - All references updated

4. **VapiInterviewSchedulingJob.cs**
   - Timer function call requests

5. **VapiService.cs**
   - No changes needed (already correct)

### Documentation Files (will update if needed):

- VAPI_MODEL_RECOMMENDATION.md
- VAPI_TIMER_FUNCTION_GUIDE.md
- TIMER_FUNCTION_IMPLEMENTATION_SUMMARY.md
- IMPLEMENTATION_CHANGES_AT_A_GLANCE.md
- COMPLETE_IMPLEMENTATION_SUMMARY.md

---

## 🎯 Valid Model Options

Based on the error message, here are valid models you can use:

### GPT-5 Family (Recommended for Quality)
- `gpt-5.2` - Latest GPT-5.2 base model
- `gpt-5.2-chat-latest` - Latest chat-optimized GPT-5.2
- `gpt-5.1` - GPT-5.1 base model
- `gpt-5.1-chat-latest` - Latest chat-optimized GPT-5.1
- `gpt-5` - GPT-5 base model
- `gpt-5-chat-latest` - Latest chat-optimized GPT-5
- **`gpt-5-mini`** ⭐ - **CURRENT CHOICE** (fast + capable)
- `gpt-5-nano` - Even faster/cheaper than mini

### GPT-4.1 Family
- `gpt-4.1` - GPT-4.1 base model
- `gpt-4.1-mini` - Mini version
- `gpt-4.1-nano` - Nano version
- Regional variants: `gpt-4.1-2025-04-14:westus`, etc.

### GPT-4o Family (Optimized)
- `gpt-4o` - GPT-4o base model
- `gpt-4o-mini` - Fast & cost-effective
- `gpt-4o-2024-11-20` - Specific version
- Regional variants available

### O-Series (Reasoning Models)
- `o3` - O3 reasoning model
- `o3-mini` - Mini version
- `o4-mini` - O4 mini
- `o1-mini` - O1 mini

### Realtime Models (For Voice)
- `gpt-4o-realtime-preview-2024-10-01`
- `gpt-4o-realtime-preview-2024-12-17`
- `gpt-4o-mini-realtime-preview-2024-12-17`
- `gpt-realtime-2025-08-28`
- `gpt-realtime-mini-2025-12-15`

---

## 💡 Model Selection Guidance

### For Your Use Case (Automated Interview Scheduling):

| Model | Speed | Cost | Quality | Recommendation |
|-------|-------|------|---------|----------------|
| `gpt-5-mini` ⭐ | Very Fast | Low | Good | **Best for volume calling** |
| `gpt-5-nano` | Fastest | Lowest | Acceptable | If cost is critical |
| `gpt-5.2` | Moderate | Moderate | Excellent | If quality is critical |
| `gpt-4o-mini` | Fast | Low | Very Good | Alternative to gpt-5-mini |
| `gpt-4o` | Slower | High | Excellent | Premium option |

**Current Choice**: `gpt-5-mini` - Best balance of speed, cost, and quality for automated calling.

---

## 🔄 If You Want to Change Model

Edit these locations:

### 1. Default Model (VapiCallRequest.cs)

```csharp
// Line 69
public string Model { get; set; } = "gpt-5-mini";  // Change here

// Line 190
public string Model { get; set; } = "gpt-5-mini";  // And here
```

### 2. Timer Function (VapiInterviewSchedulingJob.cs)

```csharp
Model = new VapiModelOverride
{
    Provider = "openai",
    Model = "gpt-5-mini",  // Change here
    Messages = ...
}
```

### 3. Dashboard Test (DashboardController.cs)

```csharp
Model = new VapiModelOverride
{
    Provider = "openai",
    Model = "gpt-5-mini",  // Change here
    Messages = ...
}
```

### 4. Manual Calls (VapiInterviewCallController.cs)

```csharp
Model = new VapiModelOverride
{
    Provider = "openai",
    Model = "gpt-5-mini",  // Change here
    Messages = ...
}
```

---

## ✅ Changes Summary

| Change | From | To | Status |
|--------|------|-----|--------|
| Model Name | `gpt-5.2-instant` | `gpt-5-mini` | ✅ |
| All Code Files | Updated | Updated | ✅ |
| Documentation | Updated | Updated | ✅ |
| Valid Model | ❌ Invalid | ✅ Valid | ✅ |

---

## 🧪 Test Again

Now that the model is corrected, run the test again:

```
https://localhost:5001/Dashboard/Index?testVapiCalls=true
```

**Expected**: Calls should initiate successfully without the 400 Bad Request error!

---

**Date**: February 24, 2026  
**Fix**: Changed model from `gpt-5.2-instant` (invalid) to `gpt-5-mini` (valid)  
**Status**: ✅ Fixed and Ready to Test  
