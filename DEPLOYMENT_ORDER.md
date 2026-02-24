# Deployment Order - Vapi Interview Scheduling System

## ⚠️ CRITICAL: Follow This Exact Order

Deploying in the wrong order will cause errors. Follow these steps precisely.

---

## 📋 Pre-Deployment Verification

Run this query to check current state:

```sql
-- Check if required columns exist
SELECT 
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE Name = 'CandidateMasterId' AND Object_ID = Object_ID('Candidate')
    ) THEN '✅ OK' 
    ELSE '❌ MUST run Step 0' END AS CandidateMasterId_Status,
    
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE Name = 'IRFId' AND Object_ID = Object_ID('VapiCallLogs')
    ) THEN '✅ OK' 
    ELSE '❌ MUST run Step 1' END AS VapiCallLogs_IRFId_Status;
```

---

## 🚀 Deployment Steps (IN ORDER)

### Step 0: Verify CandidateMasterId Column Exists (CRITICAL)

**Check if column exists**:
```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Candidate'
AND COLUMN_NAME = 'CandidateMasterId';
```

**If returns 0 rows (column doesn't exist)**:
- Run: `Risebird.DB/Migrations/Add_CandidateMasterId_To_Candidate.sql`
- This adds the CandidateMasterId column needed for the three-way join

**If returns 1 row**: Column exists, proceed to Step 1

### Step 1: Database Migration - Add IRFId Column

**File**: `Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql`

**Action**: Run this entire SQL script in your database (SSMS or Azure Portal)

**What it does**:
- Adds `IRFId INT NULL` column to VapiCallLogs
- Creates index `IX_VapiCallLogs_IRFId`
- Creates composite index `IX_VapiCallLogs_RecipientEmail_IRFId`
- Attempts to backfill IRFId from existing JSON data
- Shows summary of results

**Verification**:
```sql
-- Should return 1 row
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'VapiCallLogs' AND COLUMN_NAME = 'IRFId';

-- Should return 2 rows (both indexes)
SELECT name FROM sys.indexes
WHERE object_id = OBJECT_ID('VapiCallLogs')
AND name LIKE '%IRFId%';
```

**Time**: 5-10 seconds

---

### Step 2: Deploy Stored Procedure

**File**: `Risebird.DB/StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql`

**Action**: Run this entire SQL script in your database

**What it does**:
- Drops old version of stored procedure (if exists)
- Creates new version that groups by RecipientEmail AND IRFId
- Updates eligibility logic to track attempts per IRF

**Verification**:
```sql
-- Should return 1 row
SELECT name, create_date, modify_date
FROM sys.procedures
WHERE name = 'usp_GetEligibleCandidatesForVapiCalls';

-- Test it
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 5;
-- Should return up to 5 candidates
```

**Time**: 2-5 seconds

---

### Step 3: Build & Test Code Locally (Optional but Recommended)

**Action**: Build the solution to verify no compilation errors

```bash
cd Risebird.AzureTimerFunctions
dotnet build
```

**Verification**: Build should succeed with 0 errors

**Time**: 30-60 seconds

---

### Step 4: Deploy Code to Azure

**Option A - Via Visual Studio**:
1. Right-click on `Risebird.AzureTimerFunctions` project
2. Select "Publish"
3. Follow publish wizard

**Option B - Via Azure CLI**:
```bash
func azure functionapp publish <your-function-app-name>
```

**Option C - Via Azure DevOps/GitHub Actions**:
- Push code to repository
- CI/CD pipeline will deploy automatically

**Time**: 2-5 minutes

---

### Step 5: Verify Deployment

**Check 1**: Azure Portal Logs
- Go to Azure Portal → Function App → Log Stream
- Should see: "Vapi Interview Scheduling Job triggered at: [time]"
- If outside 9 AM - 9 PM: Should see "Skipping execution - outside business hours"

**Check 2**: Database Verification
```sql
-- Check if any calls were made by timer function
SELECT TOP 5 
    VapiCallId,
    IRFId,  -- Should NOT be NULL for new records
    RecipientEmail,
    CreatedAt,
    CreatedBy
FROM VapiCallLogs
WHERE CreatedBy = 'VapiTimerFunction'
ORDER BY CreatedAt DESC;

-- IRFId should be populated for all new records
```

**Check 3**: Stored Procedure Execution
```sql
-- Should return eligible candidates
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 10;

-- Verify logic:
-- Same email can appear multiple times if in different IRFs
-- CallAttempts should be per IRF, not global
```

**Time**: 5-10 minutes

---

## ✅ Success Criteria

After deployment, verify these:

- [ ] IRFId column exists in VapiCallLogs table
- [ ] Both IRFId indexes were created
- [ ] Stored procedure exists and runs without errors
- [ ] Code builds without errors
- [ ] Azure Function is deployed and running
- [ ] Timer triggers at expected intervals (every 15 minutes during business hours)
- [ ] New VapiCallLogs records have IRFId populated
- [ ] Same candidate can appear in eligible list for different IRFs
- [ ] Call attempts are tracked separately per IRF

---

## 🚨 Common Deployment Errors & Fixes

### Error 1: "Invalid column name 'IRFId'"

**Cause**: Code deployed before database migration

**Fix**: 
1. Run Step 1 (Add IRFId column) immediately
2. Restart Azure Function App

### Error 2: "Could not find stored procedure 'usp_GetEligibleCandidatesForVapiCalls'"

**Cause**: Stored procedure not created

**Fix**:
1. Run Step 2 (Deploy stored procedure)
2. Verify with: `SELECT * FROM sys.procedures WHERE name = 'usp_GetEligibleCandidatesForVapiCalls'`

### Error 3: "Cannot insert the value NULL into column 'IRFId'"

**Cause**: Column created as NOT NULL instead of NULL

**Fix**:
```sql
-- Make column nullable
ALTER TABLE VapiCallLogs ALTER COLUMN IRFId INT NULL;
```

### Error 4: Function runs but no candidates called

**Cause**: Time outside business hours OR no eligible candidates

**Fix**:
1. Check current time (must be 9 AM - 9 PM)
2. Run eligibility query manually to check for candidates
3. Check Azure Function logs for detailed error messages

---

## 📊 Post-Deployment Monitoring

### First Hour After Deployment

Run these queries every 15 minutes:

```sql
-- 1. Check if timer is running
SELECT TOP 3
    RecipientEmail,
    IRFId,
    CreatedAt,
    CreatedBy
FROM VapiCallLogs
WHERE CreatedBy = 'VapiTimerFunction'
ORDER BY CreatedAt DESC;

-- 2. Verify IRFId is populated
SELECT 
    COUNT(*) AS RecentCalls,
    SUM(CASE WHEN IRFId IS NOT NULL THEN 1 ELSE 0 END) AS WithIRFId,
    SUM(CASE WHEN IRFId IS NULL THEN 1 ELSE 0 END) AS WithoutIRFId
FROM VapiCallLogs
WHERE CreatedAt > DATEADD(HOUR, -1, GETUTCDATE());

-- WithoutIRFId should be 0 for new records
```

### First Day After Deployment

```sql
-- Performance check
SELECT 
    IRFId,
    COUNT(*) AS CallsToday,
    SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) AS BookingsToday,
    COUNT(DISTINCT RecipientEmail) AS UniqueCandidates
FROM VapiCallLogs
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
AND CreatedBy = 'VapiTimerFunction'
GROUP BY IRFId
ORDER BY CallsToday DESC;
```

---

## 🎯 What Changed & Why

### The Problem

**Old Logic** (INCORRECT):
```
john@example.com called 2x for IRF 101 (Java) 
→ Can't call john@example.com for IRF 102 (Python) ❌ WRONG!
```

Call attempts were tracked globally per email, preventing the same candidate from being called for different jobs.

### The Solution

**New Logic** (CORRECT):
```
john@example.com: IRF 101 → 2 attempts (max reached for Java)
john@example.com: IRF 102 → 0 attempts (can still call for Python) ✅ CORRECT!
```

Call attempts are now tracked per (email + IRFId) combination.

### Implementation

1. Added `IRFId` column to `VapiCallLogs`
2. Updated CTE in stored procedure to `GROUP BY RecipientEmail, IRFId`
3. Updated JOIN to match both email AND IRFId
4. Updated all code to save IRFId when creating call logs

---

## 📞 Example Scenarios After Deployment

### Scenario 1: Candidate in Multiple IRFs

**Data**:
- Candidate: alice@example.com
- IRF 101: Java Developer
- IRF 102: Python Developer
- IRF 103: DevOps Engineer

**First Run**:
- Timer calls alice@example.com for IRF 101 (Java)
- VapiCallLogs: `email=alice@..., IRFId=101, attempts=1`

**Second Run**:
- Timer can call alice@example.com for IRF 102 (Python) ✅
- Timer can call alice@example.com for IRF 103 (DevOps) ✅
- Timer can call alice@example.com for IRF 101 again if not booked ✅

**After 2 Calls for IRF 101**:
- Timer CANNOT call alice@example.com for IRF 101 ❌ (max reached)
- Timer CAN call alice@example.com for IRF 102 ✅ (separate tracking)
- Timer CAN call alice@example.com for IRF 103 ✅ (separate tracking)

### Scenario 2: Successful Booking Blocks Further Calls

**Data**:
- Candidate: bob@example.com
- IRF 201: Frontend Developer

**First Call**:
- Timer calls bob@example.com for IRF 201
- Bob books interview successfully
- VapiCallLogs: `email=bob@..., IRFId=201, attempts=1, InterviewBooked=1`

**Second Run**:
- Timer CANNOT call bob@example.com for IRF 201 ❌ (already booked)
- Even though attempts < 2, booking success prevents further calls

---

## 🎊 Deployment Complete Checklist

After following all steps above:

- [ ] IRFId column added to VapiCallLogs
- [ ] Indexes created successfully
- [ ] Stored procedure deployed and tested
- [ ] Code built successfully
- [ ] Code deployed to Azure
- [ ] Function triggered at least once
- [ ] New VapiCallLogs records have IRFId populated
- [ ] Verified same email can appear for different IRFs
- [ ] Monitoring queries run successfully

If all checked, you're ready to go! 🚀

---

**Deployment Date**: February 24, 2026  
**Critical Change**: IRFId tracking per candidate per IRF  
**Migration Required**: YES - Run before code deployment  
**Breaking Changes**: None (backward compatible)  
**Status**: ✅ Ready for Production  
