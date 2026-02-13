# Duplicate Entries Fix - Typing Assessment

## Problem Statement
Duplicate entries were being created in two tables:
1. **`BotInterviewQuestionVideos`** - Two entries for the same typing question
2. **`TypingSpeedAssessment`** - Two entries for the same SessionId + QuestionId
3. **Report.cshtml** - Displaying duplicate assessment details

---

## Root Cause Analysis

### Why Duplicates Were Created

**1. No Duplicate Check in Stored Procedures**
- `usp_SaveBotInterviewQuestionVideos` - Always did INSERT, never checked for existing records
- `usp_SaveTypingSpeedAssessment` - Always did INSERT, never checked for existing records

**2. Multiple Submission Scenarios**
- User clicking submit multiple times
- Network issues causing retries
- Browser back/forward navigation
- Form resubmission prompts

**3. No Database Constraints**
- No unique indexes to prevent duplicates
- Database allowed multiple identical entries

---

## Solution Implemented

### 1. Smart Upsert Logic in Stored Procedures

#### Updated: `usp_SaveBotInterviewQuestionVideos`

**File:** `Risebird.DB\StoredProcedures\usp_SaveBotInterviewQuestionVideos.sql`

**Logic:**
```sql
-- Check if record exists
IF EXISTS (
    SELECT 1 
    FROM BotInterviewQuestionVideos 
    WHERE SessionId = @SessionId 
      AND DeliveryHeadQuestionId = @DeliveryHeadQuestionId
      AND DeliveryHeadQuestionId IS NOT NULL
)
BEGIN
    -- UPDATE existing record
    UPDATE BotInterviewQuestionVideos SET ...
END
ELSE
BEGIN
    -- INSERT new record
    INSERT INTO BotInterviewQuestionVideos ...
END
```

**Behavior:**
- First submission: Creates new record
- Subsequent submissions: Updates existing record
- No duplicates created

---

#### Updated: `usp_SaveTypingSpeedAssessment`

**File:** `Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql`

**Logic:**
```sql
-- Check if record exists
SELECT @ExistingId = id 
FROM TypingSpeedAssessment 
WHERE SessionId = @SessionId AND QuestionId = @QuestionId;

IF @ExistingId IS NOT NULL
BEGIN
    -- UPDATE existing record
    UPDATE TypingSpeedAssessment SET ...
END
ELSE
BEGIN
    -- INSERT new record
    INSERT INTO TypingSpeedAssessment ...
END
```

**Behavior:**
- First submission: Creates new record
- Subsequent submissions: Updates existing record with latest data
- OpenAI analysis updated if resubmitted
- No duplicates created

---

### 2. Enhanced Retrieval with Deduplication

#### Updated: `usp_GetTypingSpeedAssessmentBySessionId`

**File:** `Risebird.DB\StoredProcedures\usp_GetTypingSpeedAssessmentBySessionId.sql`

**Logic:**
```sql
-- Use ROW_NUMBER to rank by most recent
WITH RankedAssessments AS (
    SELECT *, 
        ROW_NUMBER() OVER (PARTITION BY QuestionId ORDER BY CreatedAt DESC) AS RowNum
    FROM TypingSpeedAssessment
    WHERE SessionId = @SessionId
),
RankedVideos AS (
    SELECT *, 
        ROW_NUMBER() OVER (PARTITION BY DeliveryHeadQuestionId ORDER BY CreatedAt DESC) AS RowNum
    FROM BotInterviewQuestionVideos
    WHERE SessionId = @SessionId
)
SELECT ... 
FROM RankedAssessments 
LEFT JOIN RankedVideos 
WHERE RankedAssessments.RowNum = 1 AND RankedVideos.RowNum = 1
```

**Features:**
- Returns only 1 record per QuestionId (most recent)
- Joins only the most recent video record
- Works even if duplicates exist in database
- Safety net for legacy data

---

### 3. Unique Indexes to Prevent Future Duplicates

#### Added: Unique Index on `TypingSpeedAssessment`

**File:** `Risebird.DB\Tables\TypingSpeedAssessment.sql`

```sql
CREATE UNIQUE NONCLUSTERED INDEX UQ_TypingSpeedAssessment_SessionQuestion
ON TypingSpeedAssessment (SessionId, QuestionId);
```

**Effect:**
- Database enforces uniqueness at storage level
- Attempting to insert duplicate will raise error
- Combined with upsert logic, ensures only updates happen

---

#### Added: Unique Index on `BotInterviewQuestionVideos`

**File:** `Risebird.DB\Tables\BotInterviewQuestionVideos.sql`

```sql
CREATE UNIQUE NONCLUSTERED INDEX UQ_BotInterviewQuestionVideos_SessionQuestion
ON BotInterviewQuestionVideos (SessionId, DeliveryHeadQuestionId)
WHERE DeliveryHeadQuestionId IS NOT NULL;
```

**Features:**
- Filtered index (only applies when `DeliveryHeadQuestionId IS NOT NULL`)
- Doesn't affect records without question ID
- Enforces uniqueness for typed/spoken assessments

---

### 4. Cleanup Script for Existing Duplicates

**File:** `Risebird.DB\CleanupDuplicates_TypingAssessment.sql`

**Purpose:** Remove existing duplicates from production databases

**Features:**
1. **Identify Duplicates** - Reports how many duplicate groups exist
2. **Backup Option** - Optional backup before deletion
3. **Safe Deletion** - Keeps most recent record, deletes older ones
4. **Verification** - Checks that cleanup was successful
5. **Unique Constraints** - Optional step to add constraints

**Steps:**
1. Run identification queries to see duplicates
2. (Optional) Create backup tables
3. Uncomment and run DELETE statements
4. Verify no duplicates remain
5. (Optional) Add unique constraints

---

## Migration Path

### For New Installations
1. Run table scripts (already include unique indexes)
2. Run stored procedure scripts (already include upsert logic)
3. Deploy application

### For Existing Installations with Duplicates

**Step 1: Backup**
```sql
SELECT * INTO TypingSpeedAssessment_Backup FROM TypingSpeedAssessment;
SELECT * INTO BotInterviewQuestionVideos_Backup FROM BotInterviewQuestionVideos;
```

**Step 2: Identify Duplicates**
```sql
-- Run CleanupDuplicates_TypingAssessment.sql STEP 2
```

**Step 3: Remove Duplicates**
```sql
-- Uncomment and run DELETE statements in CleanupDuplicates_TypingAssessment.sql STEP 3
```

**Step 4: Update Table Scripts**
```sql
-- Run updated table scripts to add unique indexes:
Risebird.DB\Tables\TypingSpeedAssessment.sql
Risebird.DB\Tables\BotInterviewQuestionVideos.sql
```

**Step 5: Update Stored Procedures**
```sql
-- Run updated stored procedure scripts:
Risebird.DB\StoredProcedures\usp_SaveBotInterviewQuestionVideos.sql
Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql
Risebird.DB\StoredProcedures\usp_GetTypingSpeedAssessmentBySessionId.sql
```

**Step 6: Verify**
```sql
-- Check for remaining duplicates
SELECT SessionId, QuestionId, COUNT(*) 
FROM TypingSpeedAssessment 
GROUP BY SessionId, QuestionId 
HAVING COUNT(*) > 1;
-- Should return 0 rows

SELECT SessionId, DeliveryHeadQuestionId, COUNT(*) 
FROM BotInterviewQuestionVideos 
WHERE DeliveryHeadQuestionId IS NOT NULL
GROUP BY SessionId, DeliveryHeadQuestionId 
HAVING COUNT(*) > 1;
-- Should return 0 rows
```

**Step 7: Deploy Application**
- No code changes needed in application layer
- Repository already calls updated stored procedures

---

## Technical Details

### Upsert Pattern

**Traditional INSERT (Old):**
```sql
INSERT INTO Table (columns) VALUES (values);
```
**Problem:** Creates duplicates on retry

**Upsert Pattern (New):**
```sql
IF EXISTS (SELECT 1 FROM Table WHERE key_columns)
    UPDATE Table SET ...
ELSE
    INSERT INTO Table ...
```
**Benefit:** Idempotent - can be called multiple times safely

---

### ROW_NUMBER Deduplication

**Query Pattern:**
```sql
WITH Ranked AS (
    SELECT *, 
        ROW_NUMBER() OVER (PARTITION BY UniqueKey ORDER BY CreatedAt DESC) AS RowNum
    FROM Table
)
SELECT * FROM Ranked WHERE RowNum = 1
```

**Benefits:**
- Returns only most recent record per key
- Works even if duplicates exist
- No data modification required
- Safe for queries

---

### Unique Indexes

**Purpose:**
- Enforce data integrity at database level
- Prevent duplicates from being inserted
- Raise error if duplicate attempted

**Syntax:**
```sql
CREATE UNIQUE NONCLUSTERED INDEX IndexName
ON TableName (Column1, Column2);
```

**For BotInterviewQuestionVideos:**
```sql
-- Filtered index (only for records with DeliveryHeadQuestionId)
CREATE UNIQUE NONCLUSTERED INDEX UQ_BotInterviewQuestionVideos_SessionQuestion
ON BotInterviewQuestionVideos (SessionId, DeliveryHeadQuestionId)
WHERE DeliveryHeadQuestionId IS NOT NULL;
```

---

## Impact Analysis

### Before Fix

**Scenario:** User submits typing assessment twice
```
BotInterviewQuestionVideos:
1. SessionId: abc123, QuestionId: 5, VideoLink: url1
2. SessionId: abc123, QuestionId: 5, VideoLink: url2  ← Duplicate

TypingSpeedAssessment:
1. SessionId: abc123, QuestionId: 5, WPM: 32
2. SessionId: abc123, QuestionId: 5, WPM: 32  ← Duplicate

Report Display:
- Shows same assessment twice
- Confusing for users
```

### After Fix

**Scenario:** User submits typing assessment twice
```
First Submission:
- INSERT into BotInterviewQuestionVideos (id: 1)
- INSERT into TypingSpeedAssessment (id: 1)

Second Submission:
- UPDATE BotInterviewQuestionVideos (id: 1) ← Same record updated
- UPDATE TypingSpeedAssessment (id: 1) ← Same record updated

Report Display:
- Shows assessment once (most recent data)
- Clean, accurate display
```

---

## Verification Queries

### Check for Duplicates

**Typing Assessments:**
```sql
SELECT SessionId, QuestionId, COUNT(*) AS Count
FROM TypingSpeedAssessment
GROUP BY SessionId, QuestionId
HAVING COUNT(*) > 1;
-- Should return 0 rows after fix
```

**Video Records:**
```sql
SELECT SessionId, DeliveryHeadQuestionId, COUNT(*) AS Count
FROM BotInterviewQuestionVideos
WHERE DeliveryHeadQuestionId IS NOT NULL
GROUP BY SessionId, DeliveryHeadQuestionId
HAVING COUNT(*) > 1;
-- Should return 0 rows after fix
```

### View Unique Indexes

```sql
-- Check TypingSpeedAssessment indexes
SELECT name, type_desc, is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID('TypingSpeedAssessment');

-- Check BotInterviewQuestionVideos indexes
SELECT name, type_desc, is_unique, has_filter
FROM sys.indexes
WHERE object_id = OBJECT_ID('BotInterviewQuestionVideos');
```

---

## Testing Checklist

**Before Cleanup:**
- [ ] Identify existing duplicates using STEP 2 queries
- [ ] Record count of duplicates for each table
- [ ] (Optional) Create backup tables

**Cleanup:**
- [ ] Run STEP 3a DELETE statement (TypingSpeedAssessment)
- [ ] Run STEP 3b DELETE statement (BotInterviewQuestionVideos)
- [ ] Record how many rows were deleted

**After Cleanup:**
- [ ] Verify no duplicates remain (STEP 4 queries)
- [ ] Run table scripts to add unique indexes
- [ ] Run stored procedure scripts to add upsert logic
- [ ] Rebuild application
- [ ] Test double-submission scenario
- [ ] Verify report shows only one entry per question

**Regression Testing:**
- [ ] Complete new typing assessment
- [ ] Try to submit twice (should update, not duplicate)
- [ ] View report (should show once)
- [ ] Check database (should have only one record)
- [ ] Verify unique index error if manual duplicate INSERT attempted

---

## Files Modified

### Database Scripts
1. ✅ `Risebird.DB\Tables\TypingSpeedAssessment.sql` - Added unique index
2. ✅ `Risebird.DB\Tables\BotInterviewQuestionVideos.sql` - Added unique index
3. ✅ `Risebird.DB\StoredProcedures\usp_SaveBotInterviewQuestionVideos.sql` - Added upsert logic
4. ✅ `Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql` - Added upsert logic
5. ✅ `Risebird.DB\StoredProcedures\usp_GetTypingSpeedAssessmentBySessionId.sql` - Added deduplication
6. ✅ `Risebird.DB\CleanupDuplicates_TypingAssessment.sql` - NEW (cleanup script)

### Documentation
1. ✅ `DUPLICATE_ENTRIES_FIX.md` - This file

### Application Code
- ❌ No code changes required (repository already calls stored procedures)

---

## Prevention Strategy

### Database Level (Primary)
1. ✅ **Unique Indexes** - Enforces one record per SessionId+QuestionId
2. ✅ **Upsert Logic** - UPDATE if exists, INSERT if not

### Application Level (Secondary)
1. ✅ **Idempotent Operations** - Safe to call multiple times
2. ✅ **Error Handling** - Continues even if save fails

### Query Level (Safety Net)
1. ✅ **ROW_NUMBER Deduplication** - Returns only most recent if duplicates exist
2. ✅ **Ordered Results** - Most recent first

---

## Benefits

### For Data Integrity
- ✅ One record per question per session (guaranteed)
- ✅ Database-enforced constraints
- ✅ Audit trail preserved (AnswerModifiedAt/By)

### For Reporting
- ✅ Clean, accurate reports
- ✅ No duplicate displays
- ✅ Consistent data across all views

### For Development
- ✅ Idempotent operations
- ✅ Safe to retry
- ✅ No special handling needed

---

## Unique Index Details

### TypingSpeedAssessment Index

**Name:** `UQ_TypingSpeedAssessment_SessionQuestion`

**Columns:** `SessionId, QuestionId`

**Type:** UNIQUE NONCLUSTERED

**Effect:** 
- Cannot insert two records with same SessionId + QuestionId
- Attempting to do so will raise error
- Combined with upsert logic, forces UPDATE instead

---

### BotInterviewQuestionVideos Index

**Name:** `UQ_BotInterviewQuestionVideos_SessionQuestion`

**Columns:** `SessionId, DeliveryHeadQuestionId`

**Type:** UNIQUE NONCLUSTERED (FILTERED)

**Filter:** `WHERE DeliveryHeadQuestionId IS NOT NULL`

**Effect:**
- Only enforces uniqueness for records with DeliveryHeadQuestionId
- Other records (without question ID) unaffected
- Supports both typed and non-typed question scenarios

---

## Error Handling

### If Duplicate INSERT Attempted After Fix

**Scenario:** Application tries to insert duplicate

**What Happens:**
1. Stored procedure checks for existing record
2. Finds existing record
3. Performs UPDATE instead of INSERT
4. No error raised
5. Latest data saved

**Result:** Transparent to application, no errors

---

### If Manual INSERT Attempted

**Scenario:** Admin tries manual duplicate INSERT

**What Happens:**
```sql
INSERT INTO TypingSpeedAssessment (SessionId, QuestionId, ...)
VALUES ('session1', 5, ...);
-- First time: Success

INSERT INTO TypingSpeedAssessment (SessionId, QuestionId, ...)
VALUES ('session1', 5, ...);
-- Second time: Error
-- Violation of UNIQUE constraint
```

**Result:** Error prevents duplicate, data integrity maintained

---

## Summary

### Problem
- ✗ Duplicate entries in both tables
- ✗ Report showing duplicates
- ✗ Confusing for users

### Solution
- ✅ Upsert logic in stored procedures (UPDATE if exists, INSERT if not)
- ✅ ROW_NUMBER deduplication in queries
- ✅ Unique indexes prevent future duplicates
- ✅ Cleanup script removes existing duplicates

### Status
- ✅ **Prevention:** Implemented (unique indexes + upsert)
- ✅ **Detection:** Implemented (retrieval query deduplicates)
- ⏳ **Cleanup:** Script provided (admin to run)

---

## Quick Reference

### To Fix Existing Database:
1. Run: `CleanupDuplicates_TypingAssessment.sql` (uncomment DELETE sections)
2. Run: `TypingSpeedAssessment.sql` (adds unique index)
3. Run: `BotInterviewQuestionVideos.sql` (adds unique index)
4. Run: `usp_SaveBotInterviewQuestionVideos.sql` (adds upsert)
5. Run: `usp_SaveTypingSpeedAssessment.sql` (adds upsert)
6. Run: `usp_GetTypingSpeedAssessmentBySessionId.sql` (adds deduplication)

### To Verify Fix:
```sql
-- Should return 0 duplicates
SELECT SessionId, QuestionId, COUNT(*) 
FROM TypingSpeedAssessment 
GROUP BY SessionId, QuestionId 
HAVING COUNT(*) > 1;
```

---

---

## Related Documentation

For duplicate prevention in other tables, see:
- **`DUPLICATE_TRANSCRIPTS_FIX.md`** - Duplicate prevention for TransCriptsConv table

---

**Last Updated:** January 30, 2026  
**Version:** 1.0  
**Status:** Fix Implemented ✓
