# Typing Assessment Scoring & Duration Update

## Overview
This document describes the implementation of a fixed 2-minute duration and standardized passing criteria for typing speed assessments.

---

## New Requirements

### Duration
- **Fixed Duration:** 2 minutes (120 seconds)
- **Countdown Timer:** Shows time remaining (not elapsed)
- **Auto-Submit:** Test automatically submits when time expires
- **Visual Warning:** Timer turns red when < 30 seconds remaining

### Passing Criteria
- **Minimum WPM:** 30 words per minute
- **Minimum Accuracy:** 97%
- **Both Required:** Candidate must meet BOTH criteria to pass
- **Calculation:** Pass/Fail status is automatically calculated and saved

---

## Database Changes

### Updated Table: `TypingSpeedAssessment`

**New Columns Added:**
```sql
PassingWPM DECIMAL(10,2) NULL DEFAULT 30.00
PassingAccuracyPercentage DECIMAL(5,2) NULL DEFAULT 97.00
IsPassed BIT NULL
```

**Purpose:**
- `PassingWPM`: Stores the minimum WPM required (30.00)
- `PassingAccuracyPercentage`: Stores the minimum accuracy required (97.00)
- `IsPassed`: Boolean flag indicating if candidate passed (1) or failed (0)

**Migration Note:** If table already exists, ALTER TABLE commands will add these columns.

**File:** `Risebird.DB\Tables\TypingSpeedAssessment.sql`

---

### Updated Stored Procedure: `usp_SaveTypingSpeedAssessment`

**New Parameters:**
```sql
@PassingWPM DECIMAL(10,2) = 30.00,
@PassingAccuracyPercentage DECIMAL(5,2) = 97.00,
@IsPassed BIT = NULL
```

**Auto-Calculation Logic:**
```sql
-- Calculate pass/fail if not provided
IF @IsPassed IS NULL
BEGIN
    SET @IsPassed = CASE 
        WHEN @WordsPerMinute >= @PassingWPM 
         AND @AccuracyPercentage >= @PassingAccuracyPercentage 
        THEN 1 
        ELSE 0 
    END
END
```

**File:** `Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql`

---

## Backend Changes

### Updated Entity: `TypingSpeedAssessment.cs`

**New Properties:**
```csharp
public decimal PassingWPM { get; set; } = 30.00M;
public decimal PassingAccuracyPercentage { get; set; } = 97.00M;
public bool? IsPassed { get; set; }
```

**File:** `Risebird.Entities\ViewModel\TypingSpeedAssessment.cs`

---

### Updated View Model: `ChatMessageViewModel.cs`

**New Properties:**
```csharp
public decimal TypingPassingWPM { get; set; } = 30.00M;
public decimal TypingPassingAccuracy { get; set; } = 97.00M;
public bool? TypingIsPassed { get; set; }
```

**File:** `Risebird.Entities\ViewModel\ChatMessageViewModel.cs`

---

### Updated Repository: `AIInterviewerRepository.cs`

**Added Parameters to Stored Procedure Call:**
```csharp
parameters.Add("@PassingWPM", obj.PassingWPM);
parameters.Add("@PassingAccuracyPercentage", obj.PassingAccuracyPercentage);
parameters.Add("@IsPassed", obj.IsPassed);
```

**File:** `Risebird.Repository\AIInterviewerRepository.cs`

---

### Updated Controller: `AIInterviewController.cs`

**Pass/Fail Calculation:**
```csharp
// Calculate pass/fail based on criteria: 30 WPM and 97% accuracy
bool isPassed = model.TypingWPM >= 30.00M && model.TypingAccuracy >= 97.00M;

var typingAssessment = new TypingSpeedAssessment
{
    // ... other properties
    PassingWPM = 30.00M,
    PassingAccuracyPercentage = 97.00M,
    IsPassed = isPassed,
    // ... other properties
};
```

**File:** `Risebird.Web\Controllers\AIInterviewController.cs`

---

## Frontend Changes

### Updated View: `Index.cshtml`

#### 1. Updated Instructions
**Before:**
- Basic instructions without criteria

**After:**
```html
<li>Duration: 2 minutes maximum</li>
<li>Passing Criteria: 30 WPM with 97% accuracy</li>
<li>Click "Submit" when finished or test will auto-submit after 2 minutes</li>
```

#### 2. Updated Timer Display
**Before:**
```html
Time: <span id="typingTimerValue">0:00</span>
```

**After:**
```html
Time Remaining: <span id="typingTimerValue">2:00</span>
```

#### 3. Countdown Timer Logic
**New Implementation:**
```javascript
const maxDuration = 120; // 2 minutes in seconds
timerInterval = setInterval(() => {
    const elapsed = Math.floor((Date.now() - startTime) / 1000);
    const remaining = maxDuration - elapsed;
    
    if (remaining <= 0) {
        // Time's up - auto-submit
        clearInterval(timerInterval);
        confirmBtn.click(); // Trigger submit
    } else {
        const minutes = Math.floor(remaining / 60);
        const seconds = remaining % 60;
        timerValueElement.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;
        
        // Change color to red when less than 30 seconds
        if (remaining <= 30) {
            timerValueElement.style.color = '#dc3545';
        }
    }
}, 1000);
```

**Features:**
- Counts DOWN from 2:00 to 0:00
- Auto-submits when time expires
- Turns red at 30 seconds remaining

#### 4. Pass/Fail Calculation
**New Logic:**
```javascript
// Determine pass/fail based on criteria
const passingWPM = 30.00;
const passingAccuracy = 97.00;
const isPassed = wpm >= passingWPM && accuracyData.accuracy >= passingAccuracy;
```

#### 5. Enhanced Results Display
**New Features:**
- Shows PASSED ✓ or NEEDS IMPROVEMENT status
- Color-coded status banner (green for pass, yellow for needs improvement)
- Individual criteria marked with ✓ or ✗
- Shows required thresholds next to actual scores

**Example Output:**
```
┌─────────────────────────────┐
│     PASSED ✓                │ (Green background)
│─────────────────────────────│
│ Your Results:               │
│ • Time: 95.23 seconds       │
│ • Accuracy: 98.50% (✓97%)   │ (Green check)
│ • WPM: 35.42 (✓30 WPM)     │ (Green check)
│ • Correct: 245 / 250        │
└─────────────────────────────┘
```

#### 6. New Hidden Form Fields
```javascript
{ name: 'TypingPassingWPM', value: '' },
{ name: 'TypingPassingAccuracy', value: '' },
{ name: 'TypingIsPassed', value: '' }
```

**File:** `Risebird.Web\Views\AIInterview\Index.cshtml`

---

## User Experience Flow

### Before Test
1. Typing assessment question loads
2. Modal appears with instructions including:
   - 2-minute duration
   - 30 WPM / 97% accuracy criteria
3. Candidate clicks "Start"

### During Test
1. Video recording starts automatically
2. Countdown timer displays time remaining (2:00 → 0:00)
3. Timer turns red at 30 seconds
4. Sample text displays
5. Candidate types in text area
6. Candidate can click "Submit" anytime
7. **OR** Test auto-submits at 2:00

### After Test
1. Video recording stops
2. Results modal displays:
   - **PASSED ✓** or **NEEDS IMPROVEMENT**
   - Time taken (max 120 seconds)
   - Accuracy with pass/fail indicator
   - WPM with pass/fail indicator
   - Character count
3. Candidate clicks "Continue"
4. Confirmation dialog: "Are you sure you want to submit?"
5. Form submits to backend
6. Next question loads

---

## Pass/Fail Criteria

### Passing Requirements (ALL must be met)
- ✓ Accuracy ≥ 97.00%
- ✓ WPM ≥ 30.00
- ✓ Completed within 2 minutes

### Examples

**Example 1: PASSED**
- Accuracy: 98.5%
- WPM: 35.2
- Time: 95 seconds
- **Result: PASSED ✓**

**Example 2: FAILED (Low Accuracy)**
- Accuracy: 95.0%
- WPM: 40.0
- Time: 110 seconds
- **Result: NEEDS IMPROVEMENT** (Accuracy < 97%)

**Example 3: FAILED (Low WPM)**
- Accuracy: 99.0%
- WPM: 28.0
- Time: 100 seconds
- **Result: NEEDS IMPROVEMENT** (WPM < 30)

**Example 4: FAILED (Both)**
- Accuracy: 94.0%
- WPM: 25.0
- Time: 120 seconds
- **Result: NEEDS IMPROVEMENT** (Both criteria not met)

---

## Database Queries

### View Results with Pass/Fail Status
```sql
SELECT 
    SessionId,
    QuestionId,
    WordsPerMinute,
    AccuracyPercentage,
    PassingWPM,
    PassingAccuracyPercentage,
    IsPassed,
    CASE 
        WHEN IsPassed = 1 THEN 'PASSED ✓'
        ELSE 'NEEDS IMPROVEMENT'
    END AS Status,
    TimeTakenInSeconds,
    CreatedAt
FROM TypingSpeedAssessment
ORDER BY CreatedAt DESC;
```

### Pass Rate Statistics
```sql
SELECT 
    COUNT(*) AS TotalAttempts,
    SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) AS Passed,
    SUM(CASE WHEN IsPassed = 0 THEN 1 ELSE 0 END) AS Failed,
    CAST(SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PassRate,
    AVG(WordsPerMinute) AS AvgWPM,
    AVG(AccuracyPercentage) AS AvgAccuracy
FROM TypingSpeedAssessment;
```

### Candidates Who Passed
```sql
SELECT 
    SessionId,
    WordsPerMinute,
    AccuracyPercentage,
    TimeTakenInSeconds,
    CreatedAt,
    CreatedBy AS CandidateName
FROM TypingSpeedAssessment
WHERE IsPassed = 1
ORDER BY WordsPerMinute DESC;
```

### Candidates Who Need Improvement
```sql
SELECT 
    SessionId,
    WordsPerMinute,
    AccuracyPercentage,
    CASE 
        WHEN WordsPerMinute < PassingWPM AND AccuracyPercentage < PassingAccuracyPercentage 
        THEN 'Both WPM and Accuracy'
        WHEN WordsPerMinute < PassingWPM THEN 'WPM'
        WHEN AccuracyPercentage < PassingAccuracyPercentage THEN 'Accuracy'
    END AS FailureReason,
    CreatedAt,
    CreatedBy AS CandidateName
FROM TypingSpeedAssessment
WHERE IsPassed = 0
ORDER BY CreatedAt DESC;
```

---

## Configuration Changes

If you need to modify the passing criteria in the future:

### Option 1: Update Constants (Requires Code Deployment)
**In Controller:**
```csharp
PassingWPM = 35.00M,  // Change from 30.00
PassingAccuracyPercentage = 95.00M,  // Change from 97.00
```

**In JavaScript:**
```javascript
const passingWPM = 35.00;  // Change from 30.00
const passingAccuracy = 95.00;  // Change from 97.00
```

### Option 2: Database Configuration (Recommended for Future)
Consider adding a configuration table:
```sql
CREATE TABLE TypingAssessmentConfig (
    ConfigKey VARCHAR(50) PRIMARY KEY,
    ConfigValue DECIMAL(10,2)
);

INSERT INTO TypingAssessmentConfig VALUES ('PassingWPM', 30.00);
INSERT INTO TypingAssessmentConfig VALUES ('PassingAccuracy', 97.00);
INSERT INTO TypingAssessmentConfig VALUES ('MaxDurationSeconds', 120);
```

---

## Testing Checklist

- [ ] Database table updated with new columns
- [ ] Stored procedure updated and tested
- [ ] Backend solution rebuilt
- [ ] Application restarted
- [ ] Timer counts DOWN from 2:00 to 0:00
- [ ] Timer turns red at 30 seconds
- [ ] Auto-submit works at 0:00
- [ ] Manual submit works before 2:00
- [ ] Pass status shows for WPM ≥ 30 AND Accuracy ≥ 97%
- [ ] Fail status shows when either criterion not met
- [ ] Results display shows pass/fail banner
- [ ] Individual criteria show green/red indicators
- [ ] Data saves correctly to database with IsPassed flag
- [ ] Video recording starts and stops correctly
- [ ] Form submission works after results

---

## Files Modified/Created

### Database
1. `Risebird.DB\Tables\TypingSpeedAssessment.sql` - Added 3 columns
2. `Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql` - Added parameters and logic

### Backend
1. `Risebird.Entities\ViewModel\TypingSpeedAssessment.cs` - Added 3 properties
2. `Risebird.Entities\ViewModel\ChatMessageViewModel.cs` - Added 3 properties
3. `Risebird.Repository\AIInterviewerRepository.cs` - Added parameters to SP call
4. `Risebird.Web\Controllers\AIInterviewController.cs` - Added pass/fail calculation

### Frontend
1. `Risebird.Web\Views\AIInterview\Index.cshtml` - Updated timer, results, and form fields

### Documentation
1. `TYPING_ASSESSMENT_SCORING_UPDATE.md` - This file

---

## Migration Path

### For Existing Installations

1. **Backup Database**
   ```sql
   BACKUP DATABASE [YourDatabase] TO DISK = 'path\to\backup.bak'
   ```

2. **Run Table Update**
   ```sql
   -- Run: Risebird.DB\Tables\TypingSpeedAssessment.sql
   ```

3. **Run Stored Procedure Update**
   ```sql
   -- Run: Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql
   ```

4. **Update Existing Records** (Optional)
   ```sql
   -- Retroactively calculate pass/fail for existing records
   UPDATE TypingSpeedAssessment
   SET 
       PassingWPM = 30.00,
       PassingAccuracyPercentage = 97.00,
       IsPassed = CASE 
           WHEN WordsPerMinute >= 30.00 AND AccuracyPercentage >= 97.00 
           THEN 1 
           ELSE 0 
       END
   WHERE IsPassed IS NULL;
   ```

5. **Deploy Code**
   - Build solution
   - Deploy to server
   - Restart application

6. **Verify**
   - Test with a typing assessment question
   - Check database for IsPassed values
   - Review application logs

---

**Last Updated:** January 30, 2026  
**Version:** 2.0  
**Changes:** Added 2-minute duration, 30 WPM / 97% accuracy passing criteria, countdown timer, auto-submit, pass/fail status
