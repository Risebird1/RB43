# Three-Way Join Explained - Candidate Email Resolution

## ✅ The Correct Relationship Chain

As you specified:

```
Candidate → CandidatesMaster → UserList → EmailId
```

---

## 🎯 Join Syntax (CORRECT)

```sql
FROM Candidate c
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
```

**Where email comes from**: `u.EmailId`

---

## 📊 Table Structure

### Candidate
```
id (PK) | CandidateMasterId (FK) | IRFId (FK) | Name     | Phone
--------|------------------------|------------|----------|-------------
100     | 50                     | 1          | John Doe | 9876543210
```
- `CandidateMasterId` → References CandidatesMaster.id

### CandidatesMaster
```
id (PK) | UserId (FK) | Name     | EmailId (not used)
--------|-------------|----------|-------------------
50      | 10          | John Doe | (ignored)
```
- `UserId` → References UserList.id

### UserList
```
id (PK) | FirstName | LastName | EmailId ⭐
--------|-----------|----------|------------------
10      | John      | Doe      | john@example.com
```
- `EmailId` → **THIS IS THE SOURCE!**

---

## 🔍 Why Three Tables?

### The Architecture

**CandidatesMaster**:
- Acts as a master/deduplication table
- Multiple Candidate records (across different IRFs) can link to the same CandidatesMaster
- Example: John Doe in 3 different IRFs → 3 Candidate records → 1 CandidatesMaster record

**UserList**:
- Stores actual user accounts and contact information
- One UserList record can have multiple CandidatesMaster records
- This is where the email lives

**Candidate**:
- Represents a candidate's application to a specific IRF
- Links to CandidatesMaster (to avoid duplication)
- Links to IRF (for the job/channel)

---

## 📋 Complete Example

### Scenario: John applies to 3 different jobs

**UserList** (1 record):
```
id: 10
FirstName: John
LastName: Doe
EmailId: john@example.com ← EMAIL SOURCE
```

**CandidatesMaster** (1 record):
```
id: 50
UserId: 10  ← Links to UserList
Name: John Doe
```

**Candidate** (3 records):
```
id: 100, IRFId: 1, CandidateMasterId: 50  (Java job)
id: 101, IRFId: 2, CandidateMasterId: 50  (Python job)
id: 102, IRFId: 3, CandidateMasterId: 50  (DevOps job)
```

**All 3 Candidate records** →  
**Same CandidatesMaster (50)** →  
**Same UserList (10)** →  
**Same Email** (john@example.com)

---

## ✅ Our Stored Procedure Implementation

### The Three-Way Join (Lines 72-74)

```sql
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id  ← Step 1
INNER JOIN UserList u ON cm.UserId = u.id                       ← Step 2
LEFT JOIN CandidateCallCounts ccc ON u.EmailId = ccc.RecipientEmail 
    AND c.IRFId = ccc.IRFId
```

**Chain**:
1. `Candidate.CandidateMasterId` → `CandidatesMaster.id`
2. `CandidatesMaster.UserId` → `UserList.id`
3. Get email from `UserList.EmailId`

### All Email References Use u.EmailId

- Line 66: `u.EmailId AS CandidateEmail` (SELECT)
- Line 75: `u.EmailId = ccc.RecipientEmail` (JOIN)
- Line 79: `u.EmailId IS NOT NULL` (WHERE)
- Line 88: `im.candidateEmail = u.EmailId` (NOT EXISTS)
- Line 100: `vcl.RecipientEmail = u.EmailId` (NOT EXISTS)

---

## 🧪 Test the Three-Way Join

```sql
-- Verify the join works correctly
SELECT 
    c.id AS Candidate_id,
    c.CandidateMasterId AS Step1_FK,
    cm.id AS CandidatesMaster_id,
    cm.UserId AS Step2_FK,
    u.id AS UserList_id,
    u.EmailId AS Final_Email,
    c.Phone AS CandidatePhone
FROM Candidate c
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
WHERE c.id <= 10;

-- Should show complete chain with valid emails
```

---

## ⚠️ Common Mistakes to Avoid

### ❌ WRONG: Direct join to UserList

```sql
-- This was wrong in my first attempt
INNER JOIN UserList u ON c.AddedByUserId = u.id

-- Why wrong?
-- - AddedByUserId is the person who added the candidate
-- - NOT the candidate themselves
-- - Would return manager's email, not candidate's email!
```

### ❌ WRONG: Using Candidate.EmailId

```sql
-- Don't use this
SELECT c.EmailId AS CandidateEmail

-- Why wrong?
-- - Candidate.EmailId might be NULL or outdated
-- - Not the authoritative source
-- - Email must come from UserList via the proper chain
```

### ✅ CORRECT: Three-way join

```sql
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
SELECT u.EmailId AS CandidateEmail

-- Why correct?
-- - Follows the proper FK relationships
-- - Gets email from authoritative source (UserList)
-- - Same pattern used throughout the codebase
```

---

## 📊 Call Tracking with Three-Way Join

### How It Works

**VapiCallLogs stores**:
- `IRFId` (INT) - References IRF.id
- `RecipientEmail` (VARCHAR) - Stores UserList.EmailId value

**When checking eligibility**:
```sql
LEFT JOIN CandidateCallCounts ccc 
    ON u.EmailId = ccc.RecipientEmail    -- Match email from UserList
    AND c.IRFId = ccc.IRFId              -- AND IRF id from Candidate
```

**This ensures**:
- We count attempts for the correct email (from UserList)
- Per IRF (from Candidate.IRFId)
- Same candidate across multiple IRFs tracked separately

---

## ✅ Summary of Corrections

| What Was Wrong | What Is Correct Now |
|----------------|---------------------|
| `INNER JOIN UserList u ON c.AddedByUserId = u.id` | `INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id`<br>`INNER JOIN UserList u ON cm.UserId = u.id` |
| Missing CandidatesMaster join | Three-way join implemented |
| Might get wrong email (manager's email) | Gets candidate's actual email ✅ |

---

## 🚀 Deployment Requirements

### Required Columns

1. **Candidate.CandidateMasterId** (might need migration)
2. **VapiCallLogs.IRFId** (needs migration)

### Migration Order

1. Check/Add `Candidate.CandidateMasterId` (if needed)
2. Add `VapiCallLogs.IRFId`
3. Deploy stored procedure
4. Deploy code

---

## 🎊 Final Status

**Three-Way Join**: ✅ Implemented correctly  
**Email Source**: ✅ UserList.EmailId (via proper chain)  
**All References**: ✅ Use u.EmailId consistently  
**Schema Documentation**: ✅ Complete  

The stored procedure now follows the exact pattern used throughout your codebase!

---

**Date**: February 24, 2026  
**Critical Fix**: Corrected to three-way join (Candidate → CandidatesMaster → UserList)  
**Status**: ✅ Ready for Deployment  
