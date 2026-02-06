# Troubleshooting: Typing Speed Sample Text Not Loading

## Problem
The random text from the `TypingSpeedSampleTexts` table is not loading when a typing assessment question appears.

---

## Quick Diagnosis Steps

### Step 1: Run the Troubleshooting Script
Execute this SQL file to diagnose the issue:
```
Risebird.DB\TROUBLESHOOT_TypingSpeedSampleTexts.sql
```

This will check:
- ✓ Does the table exist?
- ✓ Does the table have data?
- ✓ Are any sample texts active (IsActive = 1)?
- ✓ Does the stored procedure exist?
- ✓ Can the stored procedure execute successfully?

### Step 2: Check Browser Console
1. Open your browser's Developer Tools (F12)
2. Go to the Console tab
3. Load a typing assessment question
4. Look for these log messages:

```javascript
Typing Assessment - Sample Text Length: [number]
Typing Assessment - Sample Text: [text content]
```

**If you see:**
- Length: 0 or empty text → Database issue (see Step 3)
- Configuration Error popup → Database is empty
- No messages at all → Check if question is flagged correctly

### Step 3: Check Application Logs
Look for these messages in your application logs:
- ✓ "Typing sample text loaded successfully. Length: [number]"
- ✗ "No typing sample text available in database"

---

## Common Issues & Solutions

### Issue 1: Table Doesn't Exist
**Symptoms:**
- Error in browser console
- Application logs show database errors

**Solution:**
```sql
-- Run this file:
Risebird.DB\Tables\TypingSpeedSampleTexts.sql
```

### Issue 2: Table is Empty
**Symptoms:**
- Browser console shows: "ERROR: No sample text available"
- Application logs show: "No typing sample text available in database"

**Solution:**
The table creation script should auto-insert 5 default texts. If it didn't:

```sql
-- Run the INSERT section from TypingSpeedSampleTexts.sql, or:
INSERT INTO TypingSpeedSampleTexts (SampleText, IsActive, CreatedAt, CreatedBy)
VALUES 
('The quick brown fox jumps over the lazy dog. Programming requires attention to detail and careful consideration of logic. Software development is both an art and a science that requires constant learning and adaptation.', 1, GETDATE(), 'System'),
('In the world of technology, innovation drives progress forward. Developers must balance creativity with practicality while building solutions that meet user needs. Effective communication and teamwork are essential skills in modern software development.', 1, GETDATE(), 'System'),
('Artificial intelligence and machine learning are transforming industries across the globe. These technologies enable computers to learn from data and make intelligent decisions. The future of computing lies in creating systems that can think and adapt like humans.', 1, GETDATE(), 'System'),
('Web development encompasses a wide range of technologies and frameworks. From frontend design to backend architecture, developers must master multiple disciplines. Building responsive and accessible applications requires dedication and continuous improvement.', 1, GETDATE(), 'System'),
('Data structures and algorithms form the foundation of computer science. Understanding how to efficiently store and manipulate data is crucial for building scalable systems. Problem-solving skills are developed through practice and perseverance.', 1, GETDATE(), 'System');
```

Verify:
```sql
SELECT COUNT(*) FROM TypingSpeedSampleTexts WHERE IsActive = 1;
-- Should return 5 (or more)
```

### Issue 3: No Active Sample Texts
**Symptoms:**
- Table has data but all IsActive = 0

**Solution:**
```sql
UPDATE TypingSpeedSampleTexts
SET IsActive = 1
WHERE id IN (1, 2, 3, 4, 5); -- Activate first 5 texts
```

### Issue 4: Stored Procedure Missing
**Symptoms:**
- Application logs show stored procedure errors

**Solution:**
```sql
-- Run this file:
Risebird.DB\StoredProcedures\usp_GetRandomTypingSpeedSampleText.sql
```

### Issue 5: Question Not Flagged Correctly
**Symptoms:**
- Typing modal doesn't appear at all
- Normal question flow continues

**Solution:**
```sql
-- Verify the question is flagged:
SELECT id, Question, IsTypingSpeedAssessment 
FROM SkillChannelSampleQuestions 
WHERE id = [your_question_id];

-- If IsTypingSpeedAssessment is 0 or NULL:
UPDATE SkillChannelSampleQuestions 
SET IsTypingSpeedAssessment = 1 
WHERE id = [your_question_id];
```

### Issue 6: Backend Not Rebuilt
**Symptoms:**
- Database is correct but still not working
- Old code still running

**Solution:**
1. Clean and rebuild the solution in Visual Studio
2. Stop IIS/Application Pool
3. Restart the application

---

## Debugging Checklist

Use this checklist to systematically identify the issue:

- [ ] Database table exists (`TypingSpeedSampleTexts`)
- [ ] Table has at least 1 row
- [ ] At least 1 row has `IsActive = 1`
- [ ] Sample texts are not empty/NULL
- [ ] Stored procedure exists (`usp_GetRandomTypingSpeedSampleText`)
- [ ] Stored procedure executes without errors
- [ ] Question is flagged with `IsTypingSpeedAssessment = 1`
- [ ] Backend solution has been rebuilt
- [ ] Application has been restarted
- [ ] Browser console shows sample text length > 0
- [ ] Application logs show "loaded successfully" message

---

## Testing the Fix

After resolving database issues:

1. **Test the stored procedure directly:**
```sql
EXEC usp_GetRandomTypingSpeedSampleText;
-- Should return a random sample text
```

2. **Test multiple executions:**
```sql
-- Run this 5 times, you should see different texts
EXEC usp_GetRandomTypingSpeedSampleText;
```

3. **Test in application:**
   - Mark a question as typing assessment
   - Start an AI interview
   - Navigate to that question
   - Modal should appear with sample text
   - Browser console should show text length > 0

---

## What I've Added for Debugging

### 1. Frontend Validation (Index.cshtml)
```javascript
// Logs sample text info to console
console.log('Typing Assessment - Sample Text Length:', sampleText ? sampleText.length : 0);
console.log('Typing Assessment - Sample Text:', sampleText);

// Shows error if no text available
if (!sampleText || sampleText.trim() === '') {
    Swal.fire({
        title: 'Configuration Error',
        text: 'No typing sample text is available...',
        icon: 'error'
    });
}
```

### 2. Backend Logging (AIInterviewController.cs)
Added logging in 3 locations:
```csharp
// Success log
await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug, 
    new Exception($"Typing sample text loaded successfully. Length: {typingSample.SampleText.Length}"));

// Error log
await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning, 
    new Exception("No typing sample text available in database..."));
```

### 3. SQL Diagnostic Script
- `TROUBLESHOOT_TypingSpeedSampleTexts.sql` - Comprehensive database check

---

## Expected Behavior

When working correctly:

1. **Database:** Table has 5+ active sample texts
2. **Backend:** Controller fetches random text, logs success
3. **Frontend:** 
   - Console shows text length (200-400 chars)
   - Console shows actual text content
   - Modal displays with text visible
   - No error messages

---

## Still Not Working?

If you've completed all steps and it's still not working:

1. **Check connection string** - Ensure the application is connected to the correct database
2. **Check user permissions** - Database user needs SELECT permission on `TypingSpeedSampleTexts`
3. **Clear browser cache** - Force refresh (Ctrl+F5)
4. **Check for JavaScript errors** - Any other errors blocking execution?
5. **Verify model binding** - Check if `Model.IsTypingSpeedQuestion` is true

---

## Files to Check

1. Database:
   - `Risebird.DB\Tables\TypingSpeedSampleTexts.sql`
   - `Risebird.DB\StoredProcedures\usp_GetRandomTypingSpeedSampleText.sql`
   - `Risebird.DB\TROUBLESHOOT_TypingSpeedSampleTexts.sql`

2. Backend:
   - `Risebird.Entities\ViewModel\TypingSpeedSampleText.cs`
   - `Risebird.Entities\ViewModel\ChatMessageViewModel.cs`
   - `Risebird.Repository\AIInterviewerRepository.cs`
   - `Risebird.Business\AIInterviewerService.cs`
   - `Risebird.Web\Controllers\AIInterviewController.cs`

3. Frontend:
   - `Risebird.Web\Views\AIInterview\Index.cshtml`

---

**Last Updated:** January 30, 2026
