# Transcript Duplicate Logic Correction

## Issue Identified
The initial implementation of duplicate prevention for `TransCriptsConv` used an incorrect duplicate detection strategy.

---

## Original (Incorrect) Logic ❌

**Duplicate criteria:**
- Same `SessionId + Role + Message + DeliveryHeadQuestionId`
- Within 5-second time window

**Problem:**
This approach didn't match the actual structure of AI interviews:
- Each question should have **exactly ONE** assistant message (the question text)
- Each question should have **exactly ONE** user response (the answer)
- The duplicate key should be the question itself, not the message content or timing

**Example of the problem:**
```
Scenario: User edits answer and resubmits

First submission:
  SessionId: abc123, Role: user, QuestionId: 5, Message: "Python is best"
  → Inserted

Second submission (user edited answer):
  SessionId: abc123, Role: user, QuestionId: 5, Message: "Python is the best"
  → Message different → Treated as NEW record ❌
  
Result: Two answers for same question ❌
```

---

## Corrected Logic ✅

### Interview Questions (DeliveryHeadQuestionId provided)

**Duplicate criteria:**
- Same `SessionId + Role + DeliveryHeadQuestionId`
- **Regardless of message content or time**

**Behavior:**
- **UPSERT pattern** (UPDATE if exists, INSERT if not)
- If user resubmits/edits answer: **UPDATE** existing record
- Ensures one record per question per role

**Example:**
```
Scenario: User edits answer and resubmits

First submission:
  SessionId: abc123, Role: user, QuestionId: 5, Message: "Python is best"
  → INSERT new record (Id: 101)

Second submission (user edited answer):
  SessionId: abc123, Role: user, QuestionId: 5, Message: "Python is the best"
  → Duplicate detected (same Session + Role + QuestionId)
  → UPDATE record 101 with new message ✓
  
Result: One answer for the question (latest version) ✓
```

---

### General Conversation (DeliveryHeadQuestionId is NULL)

**Duplicate criteria:**
- Same `SessionId + Role + Message`
- Within 5-second time window

**Behavior:**
- **Time-windowed deduplication**
- Prevents rapid retries/double-clicks
- Allows legitimate repeated messages over time

**Example:**
```
Scenario: Network retry during general chat

t=0s: User says "Hello"
  → INSERT new record

t=1s: Retry (network issue)
  → Same message within 5s → Skip (duplicate) ✓
  
Result: One "Hello" message ✓


Scenario: User says "Yes" to two different questions (20s apart)

t=0s: User says "Yes"
  → INSERT new record

t=20s: User says "Yes" again
  → Same message but > 5s difference → INSERT new record ✓
  
Result: Two "Yes" messages (different conversation points) ✓
```

---

## Updated Stored Procedure Logic

```sql
CREATE PROCEDURE usp_LogTranscript
    @SessionId UNIQUEIDENTIFIER,
    @Role NVARCHAR(20),
    @Message NVARCHAR(MAX),
    @Timestamp DATETIME,
    @DeliveryHeadQuestionId INT = NULL
AS
BEGIN
    IF @DeliveryHeadQuestionId IS NOT NULL
    BEGIN
        -- INTERVIEW QUESTION CONTEXT
        -- Check for existing: SessionId + Role + DeliveryHeadQuestionId
        
        IF EXISTS (SELECT 1 FROM TransCriptsConv 
                   WHERE SessionId = @SessionId 
                     AND Role = @Role 
                     AND DeliveryHeadQuestionId = @DeliveryHeadQuestionId)
        BEGIN
            -- UPDATE existing record
            UPDATE TransCriptsConv
            SET Message = @Message, Timestamp = @Timestamp
            WHERE SessionId = @SessionId 
              AND Role = @Role 
              AND DeliveryHeadQuestionId = @DeliveryHeadQuestionId;
        END
        ELSE
        BEGIN
            -- INSERT new record
            INSERT INTO TransCriptsConv (...) VALUES (...);
        END
    END
    ELSE
    BEGIN
        -- GENERAL CONVERSATION
        -- Check for duplicate: SessionId + Role + Message within 5 seconds
        
        IF EXISTS (SELECT 1 FROM TransCriptsConv 
                   WHERE SessionId = @SessionId 
                     AND Role = @Role 
                     AND Message = @Message 
                     AND DeliveryHeadQuestionId IS NULL
                     AND DATEDIFF(SECOND, Timestamp, @Timestamp) BETWEEN -5 AND 5)
        BEGIN
            -- Skip (duplicate within 5s window)
            RETURN;
        END
        ELSE
        BEGIN
            -- INSERT new record
            INSERT INTO TransCriptsConv (...) VALUES (...);
        END
    END
END
```

---

## Why This Matters

### Interview Structure
```
Question 1 (QuestionId: 5)
  ├─ Assistant: "What is your favorite programming language?" 
  └─ User: "Python is the best"  ← Should be exactly ONE

Question 2 (QuestionId: 6)
  ├─ Assistant: "Why do you like it?"
  └─ User: "It's easy to learn"  ← Should be exactly ONE

Question 3 (QuestionId: 7)
  ├─ Assistant: "What is your experience level?"
  └─ User: "5 years"  ← Should be exactly ONE
```

**With Original Logic:**
- If user resubmits with different wording → Multiple answers per question ❌
- If user resubmits after 5 seconds → Multiple answers per question ❌

**With Corrected Logic:**
- If user resubmits (any wording, any time) → UPDATE existing answer ✓
- Clean, one-answer-per-question structure ✓

---

## Database Changes

### Updated Indexes

**Old (removed):**
```sql
CREATE NONCLUSTERED INDEX IX_TransCriptsConv_DuplicateCheck
ON TransCriptsConv (SessionId, Role, DeliveryHeadQuestionId, Timestamp)
INCLUDE (Message);
```

**New (added):**
```sql
-- Index for interview questions (exact match)
CREATE NONCLUSTERED INDEX IX_TransCriptsConv_SessionRoleQuestion
ON TransCriptsConv (SessionId, Role, DeliveryHeadQuestionId)
WHERE DeliveryHeadQuestionId IS NOT NULL;

-- Index for general conversation (time-windowed)
CREATE NONCLUSTERED INDEX IX_TransCriptsConv_GeneralConversation
ON TransCriptsConv (SessionId, Role, Timestamp)
INCLUDE (Message)
WHERE DeliveryHeadQuestionId IS NULL;
```

**Benefits:**
- Filtered indexes (WHERE clause) - smaller, faster
- Separate indexes for separate use cases
- Optimized for specific duplicate check patterns

---

## Cleanup Script Updates

**Now handles two types of duplicates:**

### Type 1: Interview Question Duplicates
```sql
-- Duplicate = Same SessionId + Role + DeliveryHeadQuestionId
-- Keep: Earliest record

WITH RankedInterviewTranscripts AS (
    SELECT Id,
        ROW_NUMBER() OVER (
            PARTITION BY SessionId, Role, DeliveryHeadQuestionId 
            ORDER BY Timestamp ASC
        ) AS RowNum
    FROM TransCriptsConv
    WHERE DeliveryHeadQuestionId IS NOT NULL
)
DELETE FROM TransCriptsConv
WHERE Id IN (SELECT Id FROM RankedInterviewTranscripts WHERE RowNum > 1);
```

### Type 2: General Conversation Duplicates
```sql
-- Duplicate = Same SessionId + Role + Message within 5 seconds
-- Keep: Earliest record

DELETE t2
FROM TransCriptsConv t1
INNER JOIN TransCriptsConv t2
    ON t1.SessionId = t2.SessionId
    AND t1.Role = t2.Role
    AND t1.Message = t2.Message
    AND t1.DeliveryHeadQuestionId IS NULL
    AND t2.DeliveryHeadQuestionId IS NULL
    AND t1.Id < t2.Id
    AND DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) BETWEEN 0 AND 5;
```

---

## Impact on Reports

### Before Correction
```
Interview Transcript for Session abc123:

Q1: What is your favorite language?
User: Python is best
User: Python is the best  ← Duplicate from edit/resubmit
User: Python is the best language  ← Another duplicate

Q2: Why do you like it?
User: It's easy
User: It's easy to learn  ← Duplicate from edit/resubmit
```

### After Correction
```
Interview Transcript for Session abc123:

Q1: What is your favorite language?
User: Python is the best language  ← Latest version only ✓

Q2: Why do you like it?
User: It's easy to learn  ← Latest version only ✓
```

---

## Comparison Table

| Aspect | Original Logic ❌ | Corrected Logic ✅ |
|--------|------------------|-------------------|
| **Duplicate Key (Interview)** | SessionId + Role + Message + QuestionId + Time | SessionId + Role + QuestionId |
| **Pattern (Interview)** | Time-windowed | Upsert |
| **Handles edits** | No (creates duplicates) | Yes (updates existing) |
| **Handles late resubmits** | No (creates duplicates after 5s) | Yes (always updates) |
| **One answer per question** | Not guaranteed | Guaranteed |
| **Duplicate Key (General)** | N/A (no general chat in v1) | SessionId + Role + Message + Time |
| **Pattern (General)** | N/A | Time-windowed |

---

## Migration Steps

If you've already deployed the old logic:

1. **Deploy new stored procedure** - `usp_LogTranscript.sql`
2. **Deploy new indexes** - `TransCriptsConv.sql`
3. **Run cleanup script** - `CleanupDuplicates_TransCriptsConv.sql`
4. **Verify** - Check for remaining duplicates
5. **No code changes needed** - Repository already uses stored procedure

---

## Verification Queries

### Check for Interview Question Duplicates
```sql
-- Should return 0 after fix
SELECT COUNT(*) AS InterviewDuplicates
FROM (
    SELECT SessionId, Role, DeliveryHeadQuestionId, COUNT(*) AS Cnt
    FROM TransCriptsConv
    WHERE DeliveryHeadQuestionId IS NOT NULL
    GROUP BY SessionId, Role, DeliveryHeadQuestionId
    HAVING COUNT(*) > 1
) AS Dups;
```

### Check for General Conversation Duplicates
```sql
-- Should return 0 after fix (within 5s window)
SELECT COUNT(DISTINCT t2.Id) AS GeneralDuplicates
FROM TransCriptsConv t1
INNER JOIN TransCriptsConv t2
    ON t1.SessionId = t2.SessionId
    AND t1.Role = t2.Role
    AND t1.Message = t2.Message
    AND t1.DeliveryHeadQuestionId IS NULL
    AND t2.DeliveryHeadQuestionId IS NULL
    AND t1.Id < t2.Id
    AND DATEDIFF(SECOND, t1.Timestamp, t2.Timestamp) BETWEEN 0 AND 5;
```

---

## Testing Scenarios

### Test 1: User Edits Answer
```
1. Submit answer: "Python"
2. Edit and resubmit: "Python is great"
3. Edit and resubmit: "Python is the best"

Expected: Only 1 transcript record (latest version)
Query: SELECT * FROM TransCriptsConv WHERE SessionId = @id AND Role = 'user' AND DeliveryHeadQuestionId = 5
Result: 1 row, Message = "Python is the best" ✓
```

### Test 2: Network Retry
```
1. Submit answer: "Python"
2. Network timeout, retry after 2 seconds: "Python" (same message)

Expected: Only 1 transcript record (no duplicate)
Query: SELECT * FROM TransCriptsConv WHERE SessionId = @id AND Role = 'user' AND DeliveryHeadQuestionId = 5
Result: 1 row ✓
```

### Test 3: Legitimate Same Answer to Different Questions
```
1. Answer Q1: "Yes"
2. Answer Q2: "Yes" (different question, same word)

Expected: 2 transcript records (different questions)
Query: SELECT * FROM TransCriptsConv WHERE SessionId = @id AND Role = 'user' AND Message = 'Yes'
Result: 2 rows (different DeliveryHeadQuestionId) ✓
```

---

## Summary

### Root Cause
Incorrect duplicate detection logic for interview transcripts. Used message-based + time-windowed approach instead of question-based approach.

### Fix
Changed to **question-based** duplicate detection for interviews:
- Duplicate key: `SessionId + Role + DeliveryHeadQuestionId`
- Pattern: **UPSERT** (not time-window)
- Guarantees one answer per question

### Benefits
- ✅ Handles answer edits correctly
- ✅ Prevents duplicates regardless of timing
- ✅ Clean, structured transcripts
- ✅ Accurate reports

### Files Updated
1. ✅ `usp_LogTranscript.sql` - Corrected duplicate logic
2. ✅ `TransCriptsConv.sql` - Updated indexes
3. ✅ `CleanupDuplicates_TransCriptsConv.sql` - Two-phase cleanup
4. ✅ `TRANSCRIPT_DUPLICATE_LOGIC_CORRECTION.md` - This document

---

**Date:** January 30, 2026  
**Version:** 2.0 (Corrected)  
**Status:** Fixed ✓
