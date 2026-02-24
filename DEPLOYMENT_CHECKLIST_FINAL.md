# Final Deployment Checklist - Vapi Interview Scheduling

## 🎯 Critical Changes Made

1. **IRFId Column Added** to VapiCallLogs (tracks calls per candidate per IRF)
2. **Three-Way Join Corrected** (Candidate → CandidatesMaster → UserList → Email)
3. **IRF Relationships Fixed** (im.IRFId = irf.IRFId for InterviewMeetings)
4. **Email Source Corrected** (UserList.EmailId only, via proper FK chain)

---

## ⚠️ CRITICAL: Run Migrations in This EXACT Order

### Pre-Deployment Verification

```sql
-- Check what migrations are needed
SELECT 
    'CandidateMasterId in Candidate' AS Migration,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE Name = 'CandidateMasterId' AND Object_ID = Object_ID('Candidate')
    ) THEN '✅ Already exists' 
    ELSE '❌ MUST RUN Migration 1' END AS Status

UNION ALL

SELECT 
    'IRFId in VapiCallLogs' AS Migration,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE Name = 'IRFId' AND Object_ID = Object_ID('VapiCallLogs')
    ) THEN '✅ Already exists' 
    ELSE '❌ MUST RUN Migration 2' END AS Status;
```

---

## 🚀 Migration Steps

### Migration 1: Add CandidateMasterId (If Needed)

**File**: `Risebird.DB/Migrations/Add_CandidateMasterId_To_Candidate.sql`

**Run this if** pre-deployment check shows ❌ for CandidateMasterId

**What it does**:
- Adds `CandidateMasterId INT NULL` to Candidate table
- Creates index for performance
- Shows summary

**Time**: 5 seconds

**Verify**:
```sql
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Candidate'
AND COLUMN_NAME = 'CandidateMasterId';

-- Should return: CandidateMasterId | int
```

---

### Migration 2: Add IRFId to VapiCallLogs

**File**: `Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql`

**Always run this** (required for new functionality)

**What it does**:
- Adds `IRFId INT NULL` to VapiCallLogs
- Creates indexes (`IX_VapiCallLogs_IRFId`, `IX_VapiCallLogs_RecipientEmail_IRFId`)
- Attempts backfill from JSON
- Shows summary

**Time**: 10 seconds

**Verify**:
```sql
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'VapiCallLogs'
AND COLUMN_NAME = 'IRFId';

-- Should return: IRFId | int

-- Check indexes
SELECT name FROM sys.indexes
WHERE object_id = OBJECT_ID('VapiCallLogs')
AND name LIKE '%IRFId%';

-- Should return 2 indexes
```

---

### Migration 3: Deploy Stored Procedure

**File**: `Risebird.DB/StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql`

**Always run this** (updated with correct joins)

**What it does**:
- Drops old version
- Creates new version with three-way join
- Groups by email AND IRFId

**Time**: 5 seconds

**Verify**:
```sql
-- Test the stored procedure
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 5;

-- Should return up to 5 eligible candidates with:
-- - CandidateId
-- - IRFId (integer)
-- - IRFName (varchar - IRF.IRFId)
-- - CandidateEmail (from UserList.EmailId)
-- - CandidatePhone
-- - CallAttempts
```

---

## ✅ The Corrected Query

### Three-Way Join for Email

```sql
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id                        -- Get IRF details
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id -- Step 1
INNER JOIN UserList u ON cm.UserId = u.id                     -- Step 2 → Email!
LEFT JOIN CandidateCallCounts ccc ON u.EmailId = ccc.RecipientEmail 
    AND c.IRFId = ccc.IRFId
```

**Email Path**:
```
Candidate.CandidateMasterId (100 → 50)
    ↓
CandidatesMaster.id (50), UserId (50 → 10)
    ↓
UserList.id (10), EmailId → "john@example.com" ✅
```

### Why This is Correct

1. **No COALESCE**: Single source of truth for email (UserList.EmailId)
2. **Proper FKs**: Follows the actual foreign key relationships
3. **Deduplication**: Same CandidatesMaster can be used by multiple Candidate records
4. **Consistent**: Matches pattern used in all other stored procedures

---

## 📋 All Relationships in Query

| Line | Join Type | Left Table | Left Column | Right Table | Right Column | Purpose |
|------|-----------|------------|-------------|-------------|--------------|---------|
| 72 | INNER | Candidate | IRFId | IRF | id | Get IRF details |
| 73 | INNER | Candidate | CandidateMasterId | CandidatesMaster | id | Link to master |
| 74 | INNER | CandidatesMaster | UserId | UserList | id | Get email |
| 75-76 | LEFT | UserList | EmailId | VapiCallLogs CTE | RecipientEmail | Count attempts |

### Why INNER JOINs?

All three joins are INNER because:
- No IRF → Can't proceed (need job details)
- No CandidatesMaster → No way to get email
- No UserList → No email to call

If any link is broken, candidate is not callable (excluded from results).

---

## 🧪 Complete Test Script

```sql
-- ========================================
-- COMPLETE TEST FOR THREE-WAY JOIN
-- ========================================

-- Step 1: Create test user
DELETE FROM UserList WHERE EmailId LIKE 'test_3way_%@example.com';
INSERT INTO UserList (idProfile, FirstName, LastName, EmailId, IsActive)
VALUES (1, 'Test', 'User', 'test_3way_' + CAST(NEWID() AS VARCHAR(10)) + '@example.com', 1);

DECLARE @TestEmail VARCHAR(200) = (SELECT TOP 1 EmailId FROM UserList WHERE EmailId LIKE 'test_3way_%@example.com' ORDER BY id DESC);
DECLARE @UserId INT = (SELECT id FROM UserList WHERE EmailId = @TestEmail);

SELECT '1. UserList Created' AS Step, @UserId AS UserId, @TestEmail AS Email;

-- Step 2: Create CandidatesMaster
INSERT INTO CandidatesMaster (UserId, Name, TotalInvites, Type)
VALUES (@UserId, 'Test User', 0, 'Direct');

DECLARE @CandidateMasterId INT = SCOPE_IDENTITY();
SELECT '2. CandidatesMaster Created' AS Step, @CandidateMasterId AS CandidateMasterId;

-- Step 3: Get test IRFs
DECLARE @IRF1 INT = (SELECT TOP 1 id FROM IRF ORDER BY id);
DECLARE @IRF2 INT = (SELECT TOP 1 id FROM IRF WHERE id > @IRF1 ORDER BY id);

-- Step 4: Create Candidates in 2 IRFs
INSERT INTO Candidate (IRFId, CandidateMasterId, Name, Phone, TotalInvites)
VALUES 
    (@IRF1, @CandidateMasterId, 'Test User', '9876543210', 0),
    (@IRF2, @CandidateMasterId, 'Test User', '9876543210', 0);

SELECT '3. Candidates Created' AS Step, COUNT(*) AS Count FROM Candidate WHERE CandidateMasterId = @CandidateMasterId;

-- Step 5: Simulate confirmed interview for IRF1
DECLARE @IRF1_IRFId VARCHAR(200) = (SELECT IRFId FROM IRF WHERE id = @IRF1);
INSERT INTO InterviewMeetings (IRFId, candidateEmail, interviewSlot, comfirmationStatus, zoomLink, IsEmergency, sCreatedBy, dtCreatedOn, sModifiedBy, dtModifiedOn, BackUpLink)
VALUES (@IRF1_IRFId, @TestEmail, '10:00 AM', '1', 'https://zoom.us/test', 0, 'Test', GETDATE(), 'Test', GETDATE(), 'https://backup.com');

SELECT '4. InterviewMeeting Created for IRF1' AS Step;

-- Step 6: Run stored procedure
SELECT '========== STORED PROCEDURE RESULTS ==========' AS Divider;
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 10;

-- Expected: Should return IRF2 entry ONLY
-- IRF1 should be excluded (has confirmed interview)

-- Step 7: Verify the three-way join
SELECT '========== VERIFICATION ==========' AS Divider;

SELECT 
    c.id AS CandidateId,
    c.IRFId AS Candidate_IRFId,
    c.CandidateMasterId AS Candidate_CandidateMasterId,
    cm.id AS CM_id,
    cm.UserId AS CM_UserId,
    u.id AS UserList_id,
    u.EmailId AS UserList_EmailId,
    CASE 
        WHEN c.CandidateMasterId = cm.id THEN '✅ Step 1 OK'
        ELSE '❌ Step 1 Failed'
    END AS Step1_Check,
    CASE 
        WHEN cm.UserId = u.id THEN '✅ Step 2 OK'
        ELSE '❌ Step 2 Failed'
    END AS Step2_Check
FROM Candidate c
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id
WHERE c.CandidateMasterId = @CandidateMasterId;

-- All checks should show ✅

-- Step 8: Cleanup
SELECT '========== CLEANUP ==========' AS Divider;
DELETE FROM InterviewMeetings WHERE candidateEmail = @TestEmail;
DELETE FROM Candidate WHERE CandidateMasterId = @CandidateMasterId;
DELETE FROM CandidatesMaster WHERE id = @CandidateMasterId;
DELETE FROM UserList WHERE id = @UserId;

SELECT 'Test data cleaned up' AS Step;
```

---

## 🎯 Success Criteria

After deployment, verify ALL of these:

### Database Structure
- [ ] CandidateMasterId exists in Candidate table
- [ ] IRFId exists in VapiCallLogs table
- [ ] Both indexes created on VapiCallLogs

### Stored Procedure
- [ ] Runs without errors
- [ ] Returns candidates with valid emails
- [ ] Uses three-way join (Candidate → CandidatesMaster → UserList)
- [ ] Tracks attempts per email per IRFId

### Data Quality
- [ ] Returned emails are from UserList, not Candidate.EmailId
- [ ] Phone numbers from Candidate.Phone
- [ ] IRFName shows IRF.IRFId (business identifier)

### Business Logic
- [ ] Same email can appear for different IRFs
- [ ] Max 2 calls per email per IRF
- [ ] Confirmed interviews properly excluded

---

## 📊 Post-Deployment Verification

### Query 1: Verify Three-Way Join Works

```sql
-- Check if all candidates have valid email chain
SELECT 
    COUNT(*) AS TotalCandidates,
    SUM(CASE WHEN u.EmailId IS NOT NULL THEN 1 ELSE 0 END) AS WithValidEmail,
    SUM(CASE WHEN u.EmailId IS NULL THEN 1 ELSE 0 END) AS WithoutValidEmail
FROM Candidate c
LEFT JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
LEFT JOIN UserList u ON cm.UserId = u.id;

-- WithoutValidEmail should be 0 or very low
```

### Query 2: Test Stored Procedure

```sql
-- Get eligible candidates
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 10;

-- Verify:
-- - All have CandidateEmail populated
-- - All have valid Phone
-- - IRFName shows business identifier (e.g., "IRF-2026-001")
-- - CallAttempts is an integer (0, 1)
```

### Query 3: Verify Email Source

```sql
-- Compare what emails the stored procedure returns
WITH EligibleCandidates AS (
    SELECT * FROM (
        EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 5
    ) AS Results
)
SELECT 
    ec.CandidateId,
    ec.CandidateEmail AS From_StoredProc,
    c.EmailId AS From_Candidate_Table,
    u.EmailId AS From_UserList,
    CASE 
        WHEN ec.CandidateEmail = u.EmailId THEN '✅ Correct (from UserList)'
        WHEN ec.CandidateEmail = c.EmailId THEN '❌ Wrong (from Candidate)'
        ELSE '❓ Unknown source'
    END AS EmailSourceCheck
FROM EligibleCandidates ec
INNER JOIN Candidate c ON ec.CandidateId = c.id
INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
INNER JOIN UserList u ON cm.UserId = u.id;

-- EmailSourceCheck should ALWAYS show '✅ Correct (from UserList)'
```

---

## 🛠️ Deployment Command Summary

### 1. Database Migrations

```sql
-- Check if CandidateMasterId exists
-- If not, run:
\i Risebird.DB/Migrations/Add_CandidateMasterId_To_Candidate.sql

-- Add IRFId to VapiCallLogs (always run)
\i Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql

-- Deploy stored procedure (always run)
\i Risebird.DB/StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql
```

### 2. Code Deployment

```bash
# Build solution
cd Risebird.AzureTimerFunctions
dotnet build

# Deploy to Azure
func azure functionapp publish <your-function-app-name>
```

---

## 📋 Relationship Summary

### The Schema (Final Understanding)

```
IRF
├─ id (INT) ←───────── Candidate.IRFId
└─ IRFId (VARCHAR) ←── InterviewMeetings.IRFId

Candidate
├─ IRFId → IRF.id
└─ CandidateMasterId → CandidatesMaster.id

CandidatesMaster
└─ UserId → UserList.id

UserList
└─ EmailId ← ⭐ EMAIL SOURCE

VapiCallLogs
├─ IRFId → IRF.id (for tracking)
└─ RecipientEmail ← Stores value from UserList.EmailId
```

### Join Pattern

```sql
Candidate c
    ├─ INNER JOIN IRF irf ON c.IRFId = irf.id
    ├─ INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
    │       └─ INNER JOIN UserList u ON cm.UserId = u.id
    │               └─ Email: u.EmailId ⭐
    └─ LEFT JOIN VapiCallLogs (via email + IRFId)
```

---

## ✅ What's Correct Now

| Aspect | Implementation | Status |
|--------|----------------|--------|
| Email Source | UserList.EmailId via three-way join | ✅ |
| IRF Join | im.IRFId = irf.IRFId (VARCHAR = VARCHAR) | ✅ |
| Call Tracking | Per (email + IRFId) combination | ✅ |
| CandidatesMaster Join | c.CandidateMasterId = cm.id | ✅ |
| UserList Join | cm.UserId = u.id | ✅ |
| Type Safety | All comparisons type-safe | ✅ |

---

## 🚨 Critical Notes

### ⚠️ CandidateMasterId Column

**This column might not exist** in older databases!

**Check before deploying**:
```sql
SELECT * FROM sys.columns 
WHERE Name = 'CandidateMasterId' 
AND Object_ID = Object_ID('Candidate');
```

**If doesn't exist**:
1. Run migration: `Add_CandidateMasterId_To_Candidate.sql`
2. **Important**: You may need to populate this column for existing records
3. Without it, INNER JOIN will return 0 results

### ⚠️ Data Population

If CandidateMasterId is NULL for many records:
```sql
-- Check how many candidates lack CandidateMasterId
SELECT 
    COUNT(*) AS Total,
    SUM(CASE WHEN CandidateMasterId IS NOT NULL THEN 1 ELSE 0 END) AS WithValue,
    SUM(CASE WHEN CandidateMasterId IS NULL THEN 1 ELSE 0 END) AS WithoutValue
FROM Candidate;

-- If WithoutValue is high, those candidates won't be called
-- You may need a data population strategy
```

---

## 🎊 Deployment Complete Checklist

- [ ] **Migration 1**: CandidateMasterId added to Candidate (if needed)
- [ ] **Migration 2**: IRFId added to VapiCallLogs
- [ ] **Migration 3**: Stored procedure deployed
- [ ] **Verification 1**: Both migrations successful
- [ ] **Verification 2**: Stored procedure runs without errors
- [ ] **Verification 3**: Returns candidates with emails from UserList
- [ ] **Verification 4**: Timer function deployed
- [ ] **Verification 5**: Timer function creates call logs with IRFId
- [ ] **Verification 6**: Call attempts tracked per email per IRF

---

## 📚 Documentation Reference

| Document | Purpose |
|----------|---------|
| **THREE_WAY_JOIN_EXPLAINED.md** | Detailed explanation of the join chain |
| **FINAL_CORRECT_RELATIONSHIPS.md** | Complete relationship mapping |
| **SCHEMA_CLARIFICATION.md** | IRF table structure details |
| **IRFID_COLUMN_ADDITION_GUIDE.md** | IRFId tracking implementation |
| **DEPLOYMENT_ORDER.md** | Step-by-step deployment guide |

---

**Date**: February 24, 2026  
**Critical Changes**: 
1. Three-way join (Candidate → CandidatesMaster → UserList)
2. IRFId tracking per candidate per IRF
3. Correct IRF.IRFId usage for InterviewMeetings

**Status**: ✅ All relationships corrected and ready for deployment!
