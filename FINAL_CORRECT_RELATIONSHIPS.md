# Final Correct Table Relationships

## ✅ The Three-Way Relationship (CORRECT)

As you specified, the correct relationship chain is:

```
Candidate → CandidatesMaster → UserList → EmailId
```

---

## 📊 Complete Schema (Corrected)

### 1. Candidate Table
```sql
CREATE TABLE Candidate (
    id INT IDENTITY(1,1) NOT NULL,
    IRFId INT NOT NULL,                    -- FK → IRF.id
    CandidateMasterId INT NULL,            -- FK → CandidatesMaster.id ⭐ KEY COLUMN!
    Name VARCHAR(200) NULL,
    Phone NVARCHAR(200) NULL,
    ...
    FOREIGN KEY (IRFId) REFERENCES IRF(id)
)
```

### 2. CandidatesMaster Table
```sql
CREATE TABLE CandidatesMaster (
    id INT IDENTITY(1,1) NOT NULL,         -- PK
    UserId INT NOT NULL,                   -- FK → UserList.id ⭐ KEY COLUMN!
    Name VARCHAR(200) NULL,
    EmailId VARCHAR(200) NULL,             -- NOT used for candidate email
    Phone NVARCHAR(200) NULL,
    ...
)
```

### 3. UserList Table
```sql
CREATE TABLE UserList (
    id INT IDENTITY(1,1) NOT NULL,         -- PK
    FirstName VARCHAR(150) NULL,
    LastName VARCHAR(150) NULL,
    EmailId VARCHAR(150) NOT NULL,         -- ⭐ FINAL SOURCE OF EMAIL!
    MobileNumber BIGINT NULL,
    ...
)
```

### 4. IRF Table
```sql
CREATE TABLE IRF (
    id INT IDENTITY(1,1) NOT NULL,         -- PK (used by Candidate.IRFId)
    IRFId VARCHAR(200) NOT NULL,           -- Business ID (used by InterviewMeetings.IRFId)
    ...
)
```

---

## 🔗 The Correct Join Pattern

### As You Specified:

```sql
FROM Candidate c
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
```

**Data Flow**:
1. Start with Candidate
2. Join to CandidatesMaster using `Candidate.CandidateMasterId`
3. Join to UserList using `CandidatesMaster.UserId`
4. Get email from `UserList.EmailId`

---

## ✅ Updated Stored Procedure

### The Correct Joins (Lines 72-74)

```sql
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
LEFT JOIN CandidateCallCounts ccc ON u.EmailId = ccc.RecipientEmail 
    AND c.IRFId = ccc.IRFId
```

### Why All INNER JOINs?

- **IRF**: Must have valid IRF (required for context)
- **CandidatesMaster**: Must have master record (required for email chain)
- **UserList**: Must have user record (required for email)
- **CandidateCallCounts**: LEFT JOIN (might be 0 attempts)

If any link in the chain is broken, the candidate shouldn't be called (no email = can't call).

---

## 📋 All Relationships in the Query

| Join | Table A | Column | Table B | Column | Type |
|------|---------|--------|---------|--------|------|
| 1 | Candidate | IRFId | IRF | id | INT = INT |
| 2 | Candidate | CandidateMasterId | CandidatesMaster | id | INT = INT |
| 3 | CandidatesMaster | UserId | UserList | id | INT = INT |
| 4 | VapiCallLogs | IRFId | Candidate | IRFId | INT = INT |
| 5 | InterviewMeetings | IRFId | IRF | IRFId | VARCHAR = VARCHAR |

---

## 🎯 Email Resolution Path

### Step-by-Step:

```
1. Start: Candidate (id: 100, CandidateMasterId: 50)
   ↓
2. Join: CandidatesMaster (id: 50, UserId: 25)
   ↓
3. Join: UserList (id: 25, EmailId: "john@example.com")
   ↓
4. Result: Email = "john@example.com"
```

**No COALESCE needed** - there's only ONE path to the email!

---

## 🧪 Verification Query

```sql
-- Verify the three-way relationship works
SELECT 
    c.id AS CandidateId,
    c.Name AS CandidateName,
    c.CandidateMasterId AS FK_To_CandidatesMaster,
    cm.id AS CandidatesMaster_id,
    cm.UserId AS FK_To_UserList,
    u.id AS UserList_id,
    u.EmailId AS Final_Email_Source,
    u.FirstName + ' ' + u.LastName AS UserFullName
FROM Candidate c
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
WHERE c.id <= 10;

-- This shows the complete chain from Candidate to Email
```

---

## ⚠️ Migration Required

If the `CandidateMasterId` column doesn't exist in your Candidate table, run:

**File**: `Risebird.DB/Migrations/Add_CandidateMasterId_To_Candidate.sql`

This will:
- Add `CandidateMasterId INT NULL` column
- Create index for performance
- Optionally add foreign key constraint

**Verify**:
```sql
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Candidate'
AND COLUMN_NAME = 'CandidateMasterId';

-- Should return: CandidateMasterId | int
```

---

## 🔍 Why This Matters

### Wrong Join (What I Had Before)

```sql
-- INCORRECT
INNER JOIN UserList u ON c.AddedByUserId = u.id
-- This gets the email of the person who added the candidate
-- NOT the candidate's own email!
```

**Problem**: 
- If Jane Manager (jane@manager.com) added John Doe to the system
- Wrong join would return jane@manager.com (manager's email)
- We'd be calling the manager, not the candidate! ❌

### Correct Join (What We Have Now)

```sql
-- CORRECT
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
-- This follows the proper chain to get candidate's email
```

**Result**:
- Candidate links to their CandidatesMaster record
- CandidatesMaster links to their UserList record
- We get the candidate's actual email from UserList ✅

---

## 📝 Complete Query Breakdown

### Line 72-74: The Three-Way Join

```sql
FROM Candidate c                                      -- Start here
INNER JOIN IRF irf ON c.IRFId = irf.id               -- Get IRF details
INNER JOIN CandidatesMaster cm                        -- Step 1: Get master record
    ON c.CandidateMasterId = cm.id
INNER JOIN UserList u                                 -- Step 2: Get user/email
    ON cm.UserId = u.id
```

### Line 75-76: Match with Call Attempts

```sql
LEFT JOIN CandidateCallCounts ccc 
    ON u.EmailId = ccc.RecipientEmail    -- Match by email from UserList
    AND c.IRFId = ccc.IRFId              -- AND by IRF id
```

### Line 66: Email in SELECT

```sql
u.EmailId AS CandidateEmail  -- Source: UserList.EmailId (end of chain)
```

### Line 88: Email in InterviewMeetings Check

```sql
WHERE im.candidateEmail = u.EmailId  -- Compare with UserList.EmailId
```

### Line 100: Email in VapiCallLogs Check

```sql
WHERE vcl.RecipientEmail = u.EmailId  -- Compare with UserList.EmailId
```

---

## ✅ All Relationships Correct

| Relationship | Join Syntax | Status |
|--------------|-------------|--------|
| Candidate → IRF | `c.IRFId = irf.id` | ✅ |
| Candidate → CandidatesMaster | `c.CandidateMasterId = cm.id` | ✅ |
| CandidatesMaster → UserList | `cm.UserId = u.id` | ✅ |
| VapiCallLogs → Candidate (IRF) | `vcl.IRFId = c.IRFId` | ✅ |
| InterviewMeetings → IRF | `im.IRFId = irf.IRFId` | ✅ |

**Email Source**: `u.EmailId` (from UserList, end of chain) ✅

---

## 🚀 Deployment Checklist (Updated)

### Step 1: Check if CandidateMasterId exists

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Candidate'
AND COLUMN_NAME = 'CandidateMasterId';
```

**If returns 0 rows**: Run migration script `Add_CandidateMasterId_To_Candidate.sql`  
**If returns 1 row**: Column already exists, proceed to Step 2

### Step 2: Run Other Migrations

```sql
-- Add IRFId to VapiCallLogs
-- Run: Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql
```

### Step 3: Deploy Stored Procedure

```sql
-- Run: Risebird.DB/StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql
```

### Step 4: Test Query

```sql
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 5;

-- Should return candidates with:
-- - Valid CandidateMasterId
-- - Valid UserId in CandidatesMaster
-- - Valid EmailId in UserList
```

---

## 📊 Example Data Flow

### Full Example

**UserList**:
```
id | FirstName | LastName | EmailId
---|-----------|----------|------------------
10 | John      | Doe      | john@example.com
```

**CandidatesMaster**:
```
id | UserId | Name     | EmailId
---|--------|----------|------------------
50 | 10     | John Doe | (not used)
```

**Candidate**:
```
id  | IRFId | CandidateMasterId | Name     | Phone
----|-------|-------------------|----------|-------------
100 | 1     | 50                | John Doe | 9876543210
101 | 2     | 50                | John Doe | 9876543210
```

**Query Resolution for Candidate 100**:
```sql
Candidate (id: 100, CandidateMasterId: 50)
    → CandidatesMaster (id: 50, UserId: 10)
        → UserList (id: 10, EmailId: "john@example.com")
            → Email: john@example.com ✅
```

---

## Summary

**Correct Joins** (as you specified):
```sql
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
```

**Email Source**: `u.EmailId` (from UserList via three-way join) ✅

**Status**: ✅ Stored procedure corrected with proper three-way relationship!

---

**Date**: February 24, 2026  
**Critical Change**: Three-way join for email resolution  
**Migration**: May need to add CandidateMasterId column to Candidate table  
**Status**: ✅ Correct and Ready  
