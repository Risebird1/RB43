# User-Friendly Error Handling Implementation

## Overview
This document describes the implementation of user-friendly error handling for the `AddInterviewerProfile` flow, specifically for errors occurring in the stored procedure `usp_SaveInterviewerPreOnboardingVerdictV2`.

## Problem Statement
Previously, when errors occurred during the save operation in `ScreenInterviewerBeforeOnboarding` action method, users only saw a generic message: **"Failed to save verdict."** This didn't provide any context about what went wrong, making it difficult for users to understand and fix issues.

## Solution
Implemented a comprehensive error handling mechanism that captures specific error messages from the stored procedure and displays them to users in a clear, understandable format.

---

## Changes Made

### 1. Created SaveOperationResult Model
**File:** `Risebird.Entities/ViewModel/SaveOperationResult.cs`

A new model to encapsulate both success status and error messages:

```csharp
public class SaveOperationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    
    // Factory methods for easy creation
    public static SaveOperationResult SuccessResult()
    public static SaveOperationResult FailureResult(string errorMessage)
}
```

**Benefits:**
- Clean API for returning operation results
- Combines success flag with descriptive error messages
- Eliminates need for out parameters or exceptions for business logic errors

---

### 2. Updated Repository Layer
**Files:**
- `Risebird.Repository.Interface/IInterviewerRepository.cs`
- `Risebird.Repository/InterviewerRepository.cs`

#### Interface Change:
```csharp
// Before
Task<bool> SaveInterviewerPreOnboardingVerdictV2(...)

// After
Task<SaveOperationResult> SaveInterviewerPreOnboardingVerdictV2(...)
```

#### Implementation Enhancements:

**a) Enhanced Error Capturing:**
```csharp
catch (SqlException ex)
{
    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
    string errorMessage = ExtractUserFriendlyErrorMessage(ex.Message);
    return SaveOperationResult.FailureResult(errorMessage);
}
```

**b) New Error Message Extraction Method:**
Added `ExtractUserFriendlyErrorMessage()` method that intelligently extracts user-friendly messages from SQL exceptions:

- **Extracts custom error messages** from stored procedure RAISERROR statements
- **Removes technical metadata** (line numbers, internal parameters)
- **Provides context-specific messages** for different error scenarios
- **Falls back to generic messages** when specific extraction fails

**Error Scenarios Handled:**
1. Email already exists with different specialization
2. Required fields (EmailId, PrimarySpecialization, PerformedBy) are NULL or empty
3. Database operation failures (INSERT/UPDATE with 0 rows affected)
4. General SQL exceptions with custom messages

---

### 3. Updated Service Layer
**Files:**
- `Risebird.Business.Interface/IInterviewerService.cs`
- `Risebird.Business/InterviewerService.cs`

#### Interface Change:
```csharp
// Before
Task<bool> SaveInterviewerPreOnboardingVerdictV2(...)

// After
Task<SaveOperationResult> SaveInterviewerPreOnboardingVerdictV2(...)
```

#### Implementation Enhancement:
```csharp
public async Task<SaveOperationResult> SaveInterviewerPreOnboardingVerdictV2(...)
{
    try
    {
        return await _interviewerRepository.SaveInterviewerPreOnboardingVerdictV2(...);
    }
    catch (Exception ex)
    {
        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        return SaveOperationResult.FailureResult("An unexpected error occurred. Please try again.");
    }
}
```

**Benefits:**
- Propagates detailed error messages from repository
- Handles unexpected exceptions gracefully
- Maintains logging for debugging purposes

---

### 4. Updated Controller Layer
**File:** `Risebird.Web/Controllers/AIScreenerController.cs`

#### Method: `ScreenInterviewerBeforeOnboarding`

**Change in Error Handling:**
```csharp
// Before
var saveOperationResult = await _interviewerService.SaveInterviewerPreOnboardingVerdictV2(...);
if (!saveOperationResult)
{
    return Json(new { 
        isfavourableVerdict = false, 
        resumeLink = uploadedFileURL, 
        errorMessage = "Failed to save verdict." 
    });
}

// After
var saveOperationResult = await _interviewerService.SaveInterviewerPreOnboardingVerdictV2(...);
if (!saveOperationResult.Success)
{
    return Json(new { 
        isfavourableVerdict = false, 
        resumeLink = uploadedFileURL, 
        errorMessage = saveOperationResult.ErrorMessage 
    });
}
```

**Benefits:**
- Users now see specific, actionable error messages
- Better user experience with clear guidance on what went wrong
- Easier troubleshooting for support teams

---

## Error Messages Catalog

### From Stored Procedure (usp_SaveInterviewerPreOnboardingVerdictV2)

| Scenario | Original SP Error | User-Friendly Message |
|----------|-------------------|----------------------|
| Email exists with different specialization | `Email {email} already exists with specialization '{existing}'. Cannot change to '{new}'. Email and PrimarySpecialization cannot be modified once set.` | Same message (already user-friendly) |
| EmailId is NULL or empty | `EmailId is NULL or empty` | "Required information is missing. Please ensure all fields are filled." |
| PrimarySpecialization is NULL or empty | `PrimarySpecialization is NULL or empty` | "Required information is missing. Please ensure all fields are filled." |
| PerformedBy is NULL or empty | `PerformedBy (CreatedBy) is NULL or empty` | "Required information is missing. Please ensure all fields are filled." |
| UPDATE with 0 rows affected | `UPDATE executed but @@ROWCOUNT = 0` | "Failed to save verdict. Please check your inputs and try again." |
| INSERT with 0 rows affected | `INSERT executed but @@ROWCOUNT = 0` | "Failed to save verdict. Please check your inputs and try again." |
| Generic SQL errors | Various technical messages | "Failed to save verdict. Please check your inputs and try again." |
| Unexpected exceptions | Stack traces, technical errors | "An unexpected error occurred. Please try again." |

---

## Flow Diagram

```
User submits interviewer profile
         ↓
Controller: ScreenInterviewerBeforeOnboarding
         ↓
Service: SaveInterviewerPreOnboardingVerdictV2
         ↓
Repository: SaveInterviewerPreOnboardingVerdictV2
         ↓
Stored Procedure: usp_SaveInterviewerPreOnboardingVerdictV2
         ↓
[If error occurs]
         ↓
RAISERROR with custom message
         ↓
SqlException caught in Repository
         ↓
ExtractUserFriendlyErrorMessage() processes error
         ↓
SaveOperationResult.FailureResult(message)
         ↓
Propagated through Service layer
         ↓
Controller returns JSON with specific errorMessage
         ↓
User sees meaningful error message
```

---

## Testing Scenarios

### 1. Duplicate Email with Different Specialization
**Action:** Submit interviewer profile with email that exists but different specialization
**Expected Result:** 
```json
{
  "isfavourableVerdict": false,
  "resumeLink": "...",
  "errorMessage": "Email john@example.com already exists with specialization 'Java Developer'. Cannot change to 'Python Developer'. Email and PrimarySpecialization cannot be modified once set."
}
```

### 2. Missing Required Fields
**Action:** Submit profile with empty EmailId
**Expected Result:**
```json
{
  "isfavourableVerdict": false,
  "resumeLink": "...",
  "errorMessage": "Required information is missing. Please ensure all fields are filled."
}
```

### 3. Database Connection Issue
**Action:** Trigger a database timeout or connection failure
**Expected Result:**
```json
{
  "isfavourableVerdict": false,
  "resumeLink": "...",
  "errorMessage": "An unexpected error occurred. Please try again."
}
```

### 4. Successful Save
**Action:** Submit valid interviewer profile
**Expected Result:**
```json
{
  "isfavourableVerdict": true,
  "ResumeLink": "...",
  "aiResponseHref": "...",
  "errorMessage": ""
}
```

---

## Benefits of This Implementation

1. **Better User Experience**
   - Clear, actionable error messages
   - Users understand what went wrong
   - Reduces support tickets

2. **Easier Troubleshooting**
   - Support teams can quickly identify issues
   - Less back-and-forth with users
   - Faster issue resolution

3. **Maintainable Code**
   - Centralized error message extraction
   - Consistent error handling pattern
   - Easy to add new error scenarios

4. **Separation of Concerns**
   - Business logic errors separate from technical errors
   - Layered error handling (Repository → Service → Controller)
   - Each layer adds value without duplication

5. **Backward Compatible**
   - No breaking changes to stored procedure
   - Existing error logging maintained
   - Graceful degradation for unknown errors

---

## Future Enhancements

1. **Localization**
   - Add multi-language support for error messages
   - Create resource files for different languages

2. **Error Codes**
   - Assign unique codes to each error type
   - Enable better tracking and analytics

3. **Retry Logic**
   - Implement automatic retry for transient failures
   - Add exponential backoff for database timeouts

4. **Enhanced Logging**
   - Add structured logging with correlation IDs
   - Track error frequency and patterns

5. **User Guidance**
   - Add "What to do next" suggestions
   - Link to help documentation for specific errors

---

## Files Modified Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `Risebird.Entities/ViewModel/SaveOperationResult.cs` | **New** | Result model for save operations |
| `Risebird.Repository.Interface/IInterviewerRepository.cs` | **Modified** | Return type changed to SaveOperationResult |
| `Risebird.Repository/InterviewerRepository.cs` | **Modified** | Added error extraction and result wrapping |
| `Risebird.Business.Interface/IInterviewerService.cs` | **Modified** | Return type changed to SaveOperationResult |
| `Risebird.Business/InterviewerService.cs` | **Modified** | Updated to use SaveOperationResult |
| `Risebird.Web/Controllers/AIScreenerController.cs` | **Modified** | Display specific error messages from result |

---

## Conclusion

This implementation significantly improves the user experience by providing clear, actionable error messages instead of generic failures. The solution is maintainable, extensible, and follows best practices for error handling in layered architectures.
