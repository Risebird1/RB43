# Typing Speed Sample Texts - Database Implementation

## Overview
This document describes the implementation of moving typing speed assessment sample texts from hardcoded JavaScript arrays to a database-driven solution. This allows administrators to manage sample texts without modifying code.

---

## Database Changes

### 1. New Table: `TypingSpeedSampleTexts`

**Purpose**: Store reusable sample texts for typing speed assessments.

**Columns**:
- `id` (int, IDENTITY, PK) - Unique identifier
- `SampleText` (nvarchar(max)) - The text that candidates will type
- `IsActive` (bit, default 1) - Whether this text is active and can be selected
- `CreatedAt` (datetime) - When the text was added
- `CreatedBy` (varchar(150)) - Who added the text
- `UpdatedAt` (datetime) - When the text was last updated
- `UpdatedBy` (varchar(150)) - Who last updated the text

**Features**:
- Supports soft deletion via `IsActive` flag
- Audit trail with created/updated timestamps and user info
- Comes pre-populated with 5 default sample texts

**File**: `Risebird.DB\Tables\TypingSpeedSampleTexts.sql`

---

### 2. New Stored Procedure: `usp_GetRandomTypingSpeedSampleText`

**Purpose**: Retrieve a random active sample text for typing assessment.

**Logic**:
```sql
SELECT TOP 1 id, SampleText, IsActive
FROM TypingSpeedSampleTexts
WHERE IsActive = 1
ORDER BY NEWID(); -- Random selection
```

**Returns**: One random active sample text or NULL if none available.

**File**: `Risebird.DB\StoredProcedures\usp_GetRandomTypingSpeedSampleText.sql`

---

## Backend Changes

### 1. New Entity: `TypingSpeedSampleText`

**Purpose**: C# class representing the database table.

**Properties**:
- `id` (int)
- `SampleText` (string)
- `IsActive` (bool)
- `CreatedAt` (DateTime)
- `CreatedBy` (string)
- `UpdatedAt` (DateTime?)
- `UpdatedBy` (string)

**File**: `Risebird.Entities\ViewModel\TypingSpeedSampleText.cs`

---

### 2. Updated: `ChatMessageViewModel`

**New Property**:
```csharp
public string TypingSampleTextFromDB { get; set; } // Sample text fetched from database
```

**Purpose**: Pass the selected sample text from controller to view.

**File**: `Risebird.Entities\ViewModel\ChatMessageViewModel.cs`

---

### 3. Repository Layer

**Interface**: `IAIInterviewerRepository`
- Added method: `Task<TypingSpeedSampleText> GetRandomTypingSpeedSampleText();`

**Implementation**: `AIInterviewerRepository`
- Calls `usp_GetRandomTypingSpeedSampleText` stored procedure
- Returns the random sample text or null
- Includes error logging

**Files**: 
- `Risebird.Repository.Interface\IAIInterviewerRepository.cs`
- `Risebird.Repository\AIInterviewerRepository.cs`

---

### 4. Service Layer

**Interface**: `IAIInterviewerService`
- Added method: `Task<TypingSpeedSampleText> GetRandomTypingSpeedSampleText();`

**Implementation**: `AIInterviewerService`
- Delegates to repository
- Includes error handling and logging

**Files**:
- `Risebird.Business.Interface\IAIInterviewerService.cs`
- `Risebird.Business\AIInterviewerService.cs`

---

### 5. Controller: `AIInterviewController`

**Changes**: Updated in **3 locations** where typing speed questions are loaded:

1. **Initial question load** (GET Index)
2. **Resume session** (GET Index)
3. **Next question after submission** (POST Index)

**Logic Added** (at each location):
```csharp
// Fetch random typing sample text from database if it's a typing speed question
if (sampleQuestion.IsTypingSpeedAssessment)
{
    var typingSample = await _aiinterviewerService.GetRandomTypingSpeedSampleText();
    if (typingSample != null)
    {
        model.TypingSampleTextFromDB = typingSample.SampleText;
    }
}
```

**File**: `Risebird.Web\Controllers\AIInterviewController.cs`

---

## Frontend Changes

### Updated: `Index.cshtml` (Typing Speed JavaScript)

**Before**:
```javascript
// Sample texts for typing assessment (randomly selected)
const sampleTexts = [
    "The quick brown fox jumps over...",
    "In the world of technology...",
    // ... more hardcoded texts
];

// Select a random sample text
const sampleText = sampleTexts[Math.floor(Math.random() * sampleTexts.length)];
```

**After**:
```javascript
// Get sample text from database (fetched in controller)
const sampleText = @Html.Raw(Json.Serialize(Model.TypingSampleTextFromDB ?? ""));
```

**Impact**: 
- Removed ~200 characters of hardcoded text arrays
- Sample text now comes from database via the model
- Fallback to empty string if no text available

**File**: `Risebird.Web\Views\AIInterview\Index.cshtml`

---

## Documentation

### New File: `HowTo_ManageTypingSpeedSampleTexts.sql`

**Contents**:
1. View all sample texts
2. Add new sample text
3. Update existing sample text
4. Deactivate/reactivate sample text
5. Delete sample text (permanent)
6. Bulk insert sample texts
7. View statistics
8. Find sample texts by keyword
9. Test random selection
10. Best practices

**Purpose**: Complete guide for administrators to manage typing sample texts without developer assistance.

**File**: `Risebird.DB\HowTo_ManageTypingSpeedSampleTexts.sql`

---

## Data Flow

```
1. Question loads as typing speed question
   ↓
2. Controller detects IsTypingSpeedAssessment = true
   ↓
3. Controller calls GetRandomTypingSpeedSampleText()
   ↓
4. Service → Repository → Stored Procedure
   ↓
5. Random sample text selected from TypingSpeedSampleTexts table
   ↓
6. Sample text returned to controller
   ↓
7. Controller sets model.TypingSampleTextFromDB
   ↓
8. View receives model with sample text
   ↓
9. JavaScript displays sample text in SweetAlert modal
   ↓
10. Candidate types the text
   ↓
11. Results calculated and saved
```

---

## Benefits of This Implementation

1. **No Code Changes for New Texts**: Administrators can add/edit texts via SQL
2. **Soft Deletion**: Inactive texts preserved for audit purposes
3. **Audit Trail**: Track who created/updated each text and when
4. **Random Selection**: Ensures variety for candidates
5. **Flexible Management**: Easy to add seasonal, role-specific, or difficulty-graded texts
6. **Centralized Storage**: All sample texts in one database table
7. **Error Handling**: Graceful fallback if no texts available
8. **Scalability**: Can easily add hundreds of sample texts

---

## Administrator Quick Start

### Add a New Sample Text:
```sql
INSERT INTO TypingSpeedSampleTexts (SampleText, IsActive, CreatedAt, CreatedBy)
VALUES (
    'Your sample text here...',
    1,
    GETDATE(),
    'YourUsername'
);
```

### Deactivate a Sample Text:
```sql
UPDATE TypingSpeedSampleTexts
SET IsActive = 0, UpdatedAt = GETDATE(), UpdatedBy = 'YourUsername'
WHERE id = 1;
```

### View Active Sample Texts:
```sql
SELECT id, LEFT(SampleText, 100) + '...' AS Preview, IsActive
FROM TypingSpeedSampleTexts
WHERE IsActive = 1;
```

---

## Testing Checklist

- [ ] Run `TypingSpeedSampleTexts.sql` to create table
- [ ] Run `usp_GetRandomTypingSpeedSampleText.sql` to create stored procedure
- [ ] Verify 5 default texts are inserted
- [ ] Build backend solution to compile new entities/methods
- [ ] Mark a question as typing speed assessment
- [ ] Start an AI interview with that question
- [ ] Verify sample text appears in modal (from database)
- [ ] Run interview multiple times to verify random selection
- [ ] Test adding new sample text via SQL
- [ ] Verify new text can be randomly selected
- [ ] Test deactivating a text
- [ ] Verify deactivated text is not selected

---

## Files Modified/Created

### Created:
1. `Risebird.DB\Tables\TypingSpeedSampleTexts.sql`
2. `Risebird.DB\StoredProcedures\usp_GetRandomTypingSpeedSampleText.sql`
3. `Risebird.Entities\ViewModel\TypingSpeedSampleText.cs`
4. `Risebird.DB\HowTo_ManageTypingSpeedSampleTexts.sql`
5. `TYPING_SPEED_DB_IMPLEMENTATION.md` (this file)

### Modified:
1. `Risebird.Entities\ViewModel\ChatMessageViewModel.cs` - Added `TypingSampleTextFromDB` property
2. `Risebird.Repository.Interface\IAIInterviewerRepository.cs` - Added method signature
3. `Risebird.Repository\AIInterviewerRepository.cs` - Implemented method
4. `Risebird.Business.Interface\IAIInterviewerService.cs` - Added method signature
5. `Risebird.Business\AIInterviewerService.cs` - Implemented method
6. `Risebird.Web\Controllers\AIInterviewController.cs` - Fetch sample text in 3 locations
7. `Risebird.Web\Views\AIInterview\Index.cshtml` - Use DB text instead of hardcoded array

---

## Maintenance Notes

- Keep at least 5-10 active sample texts for variety
- Recommended text length: 200-400 characters
- Periodically review and update texts for relevance
- Use audit fields to track changes
- Test new texts for typing difficulty before activating
- Consider adding texts for different skill levels or job roles
- Backup table before bulk deletions

---

## Future Enhancements (Optional)

1. **Difficulty Levels**: Add `DifficultyLevel` column (Easy/Medium/Hard)
2. **Categories**: Add `Category` column (Technical/General/Business)
3. **Language Support**: Add `Language` column for multilingual support
4. **Usage Tracking**: Track how many times each text is used
5. **Performance Metrics**: Track average WPM per sample text
6. **Role-Based Texts**: Link texts to specific job roles or skill channels
7. **Text Versioning**: Version control for sample texts
8. **Admin UI**: Create web interface for managing texts instead of SQL

---

**Last Updated**: January 30, 2026  
**Version**: 1.0
