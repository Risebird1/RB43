# Typing Speed Assessment Feature - Implementation Guide

## Overview
This document describes the end-to-end implementation of the Typing Speed Assessment feature for AI Interviews in the Risebird application.

## Feature Description
The typing speed assessment allows interviewers to evaluate a candidate's typing proficiency during an AI interview. One question per IRF can be flagged as a typing speed assessment question. When this question appears (order irrelevant), the candidate is presented with a typing test interface where they must type a given text. The system captures:
- Typing speed (Words Per Minute)
- Accuracy percentage
- Time taken
- Correct vs total characters

Video recording starts automatically when the candidate begins typing, just like with other interview questions.

---

## Database Changes

### 1. New Table: `TypingSpeedAssessment`

**Location:** `Risebird.DB/Tables/TypingSpeedAssessment.sql`

**Purpose:** Stores all typing speed assessment results for candidates.

**Columns:**
- `id` (int, PK, IDENTITY) - Primary key
- `SessionId` (varchar(50)) - Links to InterviewSession
- `QuestionId` (int) - Links to SkillChannelSampleQuestions
- `SampleText` (nvarchar(max)) - The text shown to the candidate
- `TypedText` (nvarchar(max)) - What the candidate actually typed
- `TimeStarted` (datetime) - When typing began
- `TimeCompleted` (datetime) - When typing finished
- `TimeTakenInSeconds` (decimal(10,2)) - Total time taken
- `TotalCharacters` (int) - Total characters in sample text
- `CorrectCharacters` (int) - Number of correctly typed characters
- `AccuracyPercentage` (decimal(5,2)) - Calculated accuracy
- `WordsPerMinute` (decimal(10,2)) - Calculated WPM
- `CreatedAt` (datetime) - Record creation timestamp
- `CreatedBy` (varchar(150)) - Who created the record

### 2. Modified Table: `SkillChannelSampleQuestions`

**Location:** `Risebird.DB/Tables/SkillChannelSampleQuestions.sql`

**Changes:**
- Added column: `IsTypingSpeedAssessment` (bit, NULL, DEFAULT 0)
- This flag indicates whether a question should trigger the typing speed assessment

### 3. New Stored Procedure: `usp_SaveTypingSpeedAssessment`

**Location:** `Risebird.DB/StoredProcedures/usp_SaveTypingSpeedAssessment.sql`

**Purpose:** Inserts typing speed assessment data into the database.

**Parameters:**
- @SessionId
- @QuestionId
- @SampleText
- @TypedText
- @TimeStarted
- @TimeCompleted
- @TimeTakenInSeconds
- @TotalCharacters
- @CorrectCharacters
- @AccuracyPercentage
- @WordsPerMinute
- @CreatedBy

**Returns:** New record ID (SCOPE_IDENTITY)

### 4. Modified Stored Procedure: `usp_GetRandomQuestionForSession`

**Location:** `Risebird.DB/StoredProcedures/usp_GetRandomQuestionForSession.sql`

**Changes:**
- Now selects `IsTypingSpeedAssessment` flag along with other question data
- Returns `ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment`

---

## Backend Changes

### 1. New Entity: `TypingSpeedAssessment`

**Location:** `Risebird.Entities/ViewModel/TypingSpeedAssessment.cs`

**Purpose:** View model for typing speed assessment data.

**Properties:**
- id, SessionId, QuestionId, SampleText, TypedText
- TimeStarted, TimeCompleted, TimeTakenInSeconds
- TotalCharacters, CorrectCharacters, AccuracyPercentage, WordsPerMinute
- CreatedAt, CreatedBy

### 2. Modified Entity: `TechSkillComments`

**Location:** `Risebird.Entities/ViewModel/TechSkillComments.cs`

**Changes:**
- Added property: `public bool IsTypingSpeedAssessment { get; set; }`

### 3. Modified Entity: `ChatMessageViewModel`

**Location:** `Risebird.Entities/ViewModel/ChatMessageViewModel.cs`

**Changes:**
Added properties for typing speed data:
```csharp
public bool IsTypingSpeedQuestion { get; set; }
public string TypingSampleText { get; set; }
public string TypedText { get; set; }
public string TypingStartTime { get; set; }
public string TypingEndTime { get; set; }
public decimal TypingTimeTaken { get; set; }
public int TypingTotalChars { get; set; }
public int TypingCorrectChars { get; set; }
public decimal TypingAccuracy { get; set; }
public decimal TypingWPM { get; set; }
```

### 4. Repository Interface: `IAIInterviewerRepository`

**Location:** `Risebird.Repository.Interface/IAIInterviewerRepository.cs`

**Changes:**
- Added method: `Task<bool> SaveTypingSpeedAssessment(TypingSpeedAssessment obj);`

### 5. Repository Implementation: `AIInterviewerRepository`

**Location:** `Risebird.Repository/AIInterviewerRepository.cs`

**Changes:**
- Implemented `SaveTypingSpeedAssessment` method
- Calls `usp_SaveTypingSpeedAssessment` stored procedure with proper parameter mapping

### 6. Service Interface: `IAIInterviewerService`

**Location:** `Risebird.Business.Interface/IAIInterviewerService.cs`

**Changes:**
- Added method: `Task<bool> SaveTypingSpeedAssessment(TypingSpeedAssessment obj);`

### 7. Service Implementation: `AIInterviewerService`

**Location:** `Risebird.Business/AIInterviewerService.cs`

**Changes:**
- Implemented `SaveTypingSpeedAssessment` method
- Includes error handling and logging

### 8. Controller: `AIInterviewController`

**Location:** `Risebird.Web/Controllers/AIInterviewController.cs`

**Changes:**

#### In `Index` GET method:
Multiple locations where `sampleQuestion` is retrieved now also set:
```csharp
model.IsTypingSpeedQuestion = sampleQuestion.IsTypingSpeedAssessment;
```

Locations:
- Line ~114 (first question load)
- Line ~265 (next question during interview)
- Line ~920 (after POST submission)

#### In `Index` POST method:
Added logic after `UploadBotInterviewVideo` call (around line 896-927):
```csharp
// Save typing speed assessment if this was a typing speed question
if (model.IsTypingSpeedQuestion && !string.IsNullOrEmpty(model.TypedText))
{
    try
    {
        var typingAssessment = new TypingSpeedAssessment
        {
            SessionId = model.SessionId.ToString(),
            QuestionId = model.CurrentQuestionId,
            SampleText = model.TypingSampleText,
            TypedText = model.TypedText,
            TimeStarted = DateTime.Parse(model.TypingStartTime),
            TimeCompleted = DateTime.Parse(model.TypingEndTime),
            TimeTakenInSeconds = model.TypingTimeTaken,
            TotalCharacters = model.TypingTotalChars,
            CorrectCharacters = model.TypingCorrectChars,
            AccuracyPercentage = model.TypingAccuracy,
            WordsPerMinute = model.TypingWPM,
            CreatedBy = model.candidateName
        };
        await _aiinterviewerService.SaveTypingSpeedAssessment(typingAssessment);
    }
    catch (Exception ex)
    {
        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        // Continue even if typing speed save fails
    }
}
```

---

## Frontend Changes

### Modified View: `Index.cshtml`

**Location:** `Risebird.Web/Views/AIInterview/Index.cshtml`

**Changes:**

#### JavaScript Addition (at end of script section, before `</script>`):

Added comprehensive typing speed assessment functionality including:

1. **Detection Logic:**
   - Checks if current question is a typing speed question: `@Json.Serialize(Model.IsTypingSpeedQuestion)`
   - Only activates when flag is true

2. **Sample Text Selection:**
   - Array of 5 pre-defined sample texts
   - Randomly selects one when the test begins
   - Can be easily expanded with more texts

3. **Typing Test Modal (SweetAlert2):**
   - **Initial State:** Shows instructions and "Start" button
   - **Active State:** Shows sample text, timer, and text area for typing
   - **Completion:** Shows results (WPM, accuracy, time taken)

4. **Metrics Calculation:**
   - **Accuracy:** Character-by-character comparison
   - **WPM:** Words divided by time in minutes
   - **Correct Characters:** Count of matching characters at same positions

5. **Video Recording Integration:**
   - Automatically calls `startRecording()` when typing begins
   - Automatically calls `stopRecording()` when test is submitted
   - No changes required to existing recording functions

6. **Data Capture:**
   - Creates hidden form fields dynamically
   - Populates fields when test completes
   - Submitted with normal form POST

7. **Hidden Form Fields Created:**
   ```html
   <input type="hidden" name="IsTypingSpeedQuestion" value="true" />
   <input type="hidden" name="TypingSampleText" value="" />
   <input type="hidden" name="TypedText" value="" />
   <input type="hidden" name="TypingStartTime" value="" />
   <input type="hidden" name="TypingEndTime" value="" />
   <input type="hidden" name="TypingTimeTaken" value="" />
   <input type="hidden" name="TypingTotalChars" value="" />
   <input type="hidden" name="TypingCorrectChars" value="" />
   <input type="hidden" name="TypingAccuracy" value="" />
   <input type="hidden" name="TypingWPM" value="" />
   ```

8. **User Experience Flow:**
   - Page loads → Modal appears after 1 second
   - Candidate clicks "Start" → Sample text and input area appear
   - Timer starts counting up
   - Video recording starts automatically
   - Candidate types the text
   - Candidate clicks "Submit" when done
   - Metrics are calculated and displayed
   - Video recording stops automatically
   - Candidate proceeds with normal interview question answer
   - All data is submitted together with the video response

---

## Usage Instructions

### For Administrators: Setting Up a Typing Speed Question

1. Navigate to the IRF (Interview Requisition Form) details
2. Go to Sample Questions section
3. For ONE question per IRF, set `IsTypingSpeedAssessment = 1` in the database
   ```sql
   UPDATE SkillChannelSampleQuestions
   SET IsTypingSpeedAssessment = 1
   WHERE id = [QuestionId] AND IRFId = [YourIRFId]
   ```
4. Ensure only ONE question per IRF has this flag set
5. The question can be in any order - the system will trigger typing test when that question appears

### For Candidates: Taking the Typing Test

1. Interview proceeds normally
2. When typing speed question appears, a modal pops up automatically
3. Read the instructions
4. Click "Start" when ready
5. Type the displayed text as accurately and quickly as possible
6. Video recording starts automatically when you begin typing
7. Click "Submit" when finished typing
8. Review your results (WPM, accuracy)
9. Click "Continue"
10. Proceed to answer the actual interview question (if applicable)
11. Submit your response as normal

### For Interviewers: Reviewing Typing Speed Results

Query the `TypingSpeedAssessment` table:
```sql
SELECT 
    tsa.*,
    biqv.question,
    im.candidateEmail
FROM TypingSpeedAssessment tsa
INNER JOIN BotInterviewQuestionVideos biqv ON biqv.DeliveryHeadQuestionId = tsa.QuestionId 
    AND biqv.SessionId = tsa.SessionId
INNER JOIN InterviewSessions isess ON isess.SessionId = CAST(tsa.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im ON im.SessionId = isess.SessionId
WHERE im.candidateEmail = '[CandidateEmail]'
```

---

## Technical Details

### Accuracy Calculation Algorithm
```javascript
function calculateAccuracy(originalText, typedText) {
    let correctChars = 0;
    const minLength = Math.min(originalText.length, typedText.length);
    
    for (let i = 0; i < minLength; i++) {
        if (originalText[i] === typedText[i]) {
            correctChars++;
        }
    }
    
    return {
        correctChars: correctChars,
        totalChars: originalText.length,
        accuracy: (correctChars / originalText.length) * 100
    };
}
```

### WPM Calculation Algorithm
```javascript
function calculateWPM(text, timeInSeconds) {
    const words = text.trim().split(/\s+/).length;
    const minutes = timeInSeconds / 60;
    return words / minutes;
}
```

### Timer Implementation
- Uses JavaScript `setInterval` with 1-second refresh
- Displays as MM:SS format
- Stops when candidate submits
- Accurate to the second

---

## Integration Points

### Existing Systems NOT Modified:
1. Video recording start/stop functions - unchanged
2. Form submission logic - unchanged
3. Question fetching flow - only extended to include new flag
4. Database transaction handling - unchanged

### New Integration Points:
1. Typing speed data is saved AFTER video upload succeeds
2. If typing speed save fails, interview continues (non-blocking)
3. All typing data is optional and won't break existing functionality

---

## Error Handling

### Backend:
- Try-catch blocks around typing speed save operations
- Errors logged but don't stop interview flow
- Failed typing speed saves won't cause interview to fail

### Frontend:
- Modal cannot be closed until test is complete (prevents accidental dismissal)
- Disabled text area until "Start" is clicked
- Form validation ensures all required fields are populated

---

## Testing Checklist

- [ ] Database tables created successfully
- [ ] Stored procedure executes without errors
- [ ] Flag question in database: `UPDATE SkillChannelSampleQuestions SET IsTypingSpeedAssessment = 1 WHERE id = X`
- [ ] Start interview, confirm typing modal appears for flagged question
- [ ] Test "Start" button functionality
- [ ] Verify timer starts and counts correctly
- [ ] Verify video recording starts automatically
- [ ] Type sample text completely
- [ ] Click "Submit" and verify results display
- [ ] Check accuracy calculation is correct
- [ ] Check WPM calculation is reasonable
- [ ] Verify video stops recording
- [ ] Complete question answer normally
- [ ] Verify typing data saved in `TypingSpeedAssessment` table
- [ ] Verify video saved in `BotInterviewQuestionVideos` table
- [ ] Test with non-typing-speed question (should work normally)
- [ ] Verify only ONE question per IRF can be flagged

---

## Future Enhancements

1. **Admin UI:** Create UI for admins to flag questions instead of SQL
2. **Custom Sample Texts:** Allow admins to configure custom typing texts per IRF
3. **Difficulty Levels:** Different text lengths/complexity for different roles
4. **Real-time Feedback:** Show WPM and accuracy while typing
5. **Replay Feature:** Allow reviewers to see typing replay in admin panel
6. **Analytics Dashboard:** Aggregate typing speed statistics across candidates
7. **Leaderboard:** Optional feature to show typing speed rankings

---

## Support and Maintenance

### Common Issues:

**Issue:** Modal doesn't appear
- **Solution:** Check browser console for JavaScript errors. Verify SweetAlert2 library is loaded.

**Issue:** Data not saving to database
- **Solution:** Check application logs. Verify stored procedure exists and has correct parameters.

**Issue:** Video not starting automatically
- **Solution:** Verify `startRecording()` and `stopRecording()` functions exist in global scope.

**Issue:** Accuracy shows 0%
- **Solution:** Check if candidate typed anything. Verify accuracy calculation logic.

### Logging:
- All errors are logged via `_applicationErrorLogger.LogMessage()`
- Check application logs for `TypingSpeedAssessment` related entries

---

## Version History

**Version 1.0** (January 30, 2026)
- Initial implementation
- Basic typing speed, accuracy, and WPM calculation
- Integration with existing video recording
- Database schema and backend API
- Frontend modal and JavaScript logic

---

## Credits

Implemented by: AI Assistant
Date: January 30, 2026
Feature Request: Assess candidate typing speed during AI interviews
