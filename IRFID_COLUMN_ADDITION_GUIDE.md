# IRFId Column Addition to VapiCallLogs

## Why This Change?

**Problem**: A candidate can exist in multiple IRFs (skill channels). The original implementation tracked call attempts globally per email address, which meant:
- If a candidate was in IRF #1 and IRF #2
- Calling them twice for IRF #1 would prevent calling them for IRF #2
- This is incorrect behavior

## ⚠️ Important: IRF Table Relationships

**Understanding the Schema**:
- `IRF.id` = INT (e.g., 1, 2, 3) - Auto-increment Primary Key
- `IRF.IRFId` = VARCHAR(200) (e.g., "IRF-2026-001") - Business identifier
- `Candidate.IRFId` = INT - References IRF.id (the integer id)
- `VapiCallLogs.IRFId` = INT - References IRF.id (the integer id)
- `InterviewMeetings.IRFId` = VARCHAR(200) - References IRF.IRFId (the varchar identifier)

**Key Point**: IRF table has TWO identifier columns. Other tables reference different columns based on their type:
- INT columns (Candidate, VapiCallLogs) → reference IRF.id
- VARCHAR columns (InterviewMeetings) → reference IRF.IRFId

**Solution**: Track call attempts per candidate per IRFId combination
- Same candidate can be called up to 2 times for IRF #1
- Same candidate can be called up to 2 times for IRF #2
- Attempts are tracked separately for each IRF

---

## Changes Made

### 1. Database Schema Update

#### VapiCallLogs Table
**File**: `Risebird.DB/Tables/VapiCallLogs.sql`

**Added**:
- `IRFId INT NULL` column
- Index: `IX_VapiCallLogs_IRFId` (for filtering by IRF)
- Composite Index: `IX_VapiCallLogs_RecipientEmail_IRFId` (for eligibility queries)

**Migration Script**: `Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql`

### 2. Entity Model Update

#### VapiCallLog Class
**File**: `Risebird.Entities/Data/VapiCallLog.cs`

**Added**:
```csharp
public int? IRFId { get; set; }
```

### 3. Stored Procedure Update

#### usp_GetEligibleCandidatesForVapiCalls
**File**: `Risebird.DB/StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql`

**Changed**: CTE now groups by both `RecipientEmail` AND `IRFId`

**Before**:
```sql
WITH CandidateCallCounts AS (
    SELECT 
        RecipientEmail,
        COUNT(*) AS CallAttempts,
        MAX(CreatedAt) AS LastCallAttempt
    FROM VapiCallLogs
    WHERE RecipientEmail IS NOT NULL
    GROUP BY RecipientEmail
)
```

**After**:
```sql
WITH CandidateCallCounts AS (
    SELECT 
        RecipientEmail,
        IRFId,
        COUNT(*) AS CallAttempts,
        MAX(CreatedAt) AS LastCallAttempt
    FROM VapiCallLogs
    WHERE RecipientEmail IS NOT NULL
        AND IRFId IS NOT NULL
    GROUP BY RecipientEmail, IRFId
)
```

**Join Updated**:
```sql
LEFT JOIN CandidateCallCounts ccc 
    ON COALESCE(c.EmailId, cm.EmailId, u.EmailId) = ccc.RecipientEmail 
    AND c.IRFId = ccc.IRFId  -- NEW: Also match by IRFId
```

**Filter Updated**:
```sql
-- Check if candidate has confirmed interview in InterviewMeetings
AND NOT EXISTS (
    SELECT 1 
    FROM InterviewMeetings im
    WHERE im.candidateEmail = COALESCE(c.EmailId, cm.EmailId, u.EmailId)
    AND im.IRFId = irf.IRFId  -- InterviewMeetings references IRF.IRFId (VARCHAR)
    AND im.comfirmationStatus = '1'
)

-- Check if candidate successfully booked via Vapi for this IRF
OR NOT EXISTS (
    SELECT 1 
    FROM VapiCallLogs vcl
    WHERE vcl.RecipientEmail = COALESCE(c.EmailId, cm.EmailId, u.EmailId)
    AND vcl.IRFId = c.IRFId  -- Both reference IRF.id (INT)
    AND vcl.InterviewBooked = 1
)
```

### 4. Repository Update

#### VapiCallRepository.cs

**SaveCallLogAsync**: Updated INSERT to include IRFId
```sql
INSERT INTO VapiCallLogs (
    VapiCallId, InterviewId, IRFId, RecipientId, ...  -- Added IRFId
)
```

**UpdateCallLogAsync**: Updated UPDATE to include IRFId
```sql
UPDATE VapiCallLogs
SET IRFId = @IRFId,  -- Added IRFId
    CallStatus = @CallStatus,
    ...
```

### 5. Controller Update

#### VapiInterviewCallController.cs

**InitiateInterviewCall**: Parse and save IRFId
```csharp
var callLog = new VapiCallLog
{
    VapiCallId = callResponse.Id,
    InterviewId = interviewId,
    IRFId = int.TryParse(irfId, out int parsedIrfId) ? parsedIrfId : (int?)null,  // NEW
    ...
};
```

**Webhook Handler**: Extract irfId from tool arguments and save
```csharp
callLog = new VapiCallLog
{
    VapiCallId = callId,
    IRFId = int.TryParse(irfId, out int parsedIrfId) ? parsedIrfId : (int?)null,  // NEW
    ...
};
```

### 6. Timer Function Update

#### VapiInterviewSchedulingJob.cs

**Save call log with IRFId**:
```csharp
var callLog = new VapiCallLog
{
    VapiCallId = callResponse.Id,
    IRFId = candidate.IRFId,  // NEW
    ...
};
```

---

## Deployment Steps

### Step 1: Run Database Migration

**Option A - Using Migration Script** (Recommended):
```sql
-- Run this script in your database
-- File: Risebird.DB/Migrations/Add_IRFId_To_VapiCallLogs.sql

-- This script:
-- 1. Adds IRFId column if not exists
-- 2. Creates necessary indexes
-- 3. Attempts to backfill existing records from JSON
-- 4. Shows summary
```

**Option B - Run VapiCallLogs.sql** (Creates table with column):
```sql
-- If table doesn't exist yet, just run VapiCallLogs.sql
-- It now includes IRFId column in CREATE TABLE statement
```

### Step 2: Verify Database Changes

```sql
-- Check if column was added
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'VapiCallLogs'
AND COLUMN_NAME = 'IRFId';

-- Should return:
-- IRFId | int | YES

-- Check indexes
SELECT 
    name AS IndexName,
    type_desc AS IndexType
FROM sys.indexes
WHERE object_id = OBJECT_ID('VapiCallLogs')
AND name LIKE '%IRFId%';

-- Should return:
-- IX_VapiCallLogs_IRFId
-- IX_VapiCallLogs_RecipientEmail_IRFId
```

### Step 3: Re-run Stored Procedure Script

```sql
-- Run the updated stored procedure
-- File: Risebird.DB/StoredProcedures/usp_GetEligibleCandidatesForVapiCalls.sql

-- This will DROP and recreate the procedure with new logic
```

### Step 4: Deploy Code Changes

```bash
# Build the solution
dotnet build

# Deploy to Azure (if using Azure Functions)
func azure functionapp publish <your-function-app-name>
```

### Step 5: Verify Deployment

```sql
-- Test the updated stored procedure
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 5;

-- Verify results include candidates from same email but different IRFs
```

---

## Example Scenario

### Before Change (INCORRECT)

**Candidate**: john@example.com

| IRFId | Job Title | Call Attempts (Global) | Can Call? |
|-------|-----------|------------------------|-----------|
| 101 | Java Dev | 2 (called twice for Java) | ❌ NO |
| 102 | Python Dev | 0 (never called for Python) | ❌ NO (blocked by Java attempts!) |

**Problem**: Can't call John for Python job because he was already called twice for Java job.

### After Change (CORRECT)

**Candidate**: john@example.com

| IRFId | Job Title | Call Attempts (Per IRF) | Can Call? |
|-------|-----------|-------------------------|-----------|
| 101 | Java Dev | 2 | ❌ NO (reached limit for this IRF) |
| 102 | Python Dev | 0 | ✅ YES (separate tracking) |

**Solution**: John can be called up to 2 times for each IRF independently.

---

## Testing the Change

### Test Case 1: Same Candidate, Multiple IRFs

```sql
-- Setup: Create test candidate in 2 IRFs
INSERT INTO Candidate (IRFId, Name, EmailId, Phone, AddedByUserId)
VALUES 
    (101, 'Test User', 'test@example.com', '9876543210', 1),
    (102, 'Test User', 'test@example.com', '9876543210', 1);

-- Simulate 2 calls for IRF 101
INSERT INTO VapiCallLogs (VapiCallId, IRFId, RecipientEmail, RecipientPhone, RecipientName, CreatedAt, CreatedBy)
VALUES 
    ('call_1', 101, 'test@example.com', '+919876543210', 'Test User', GETUTCDATE(), 'Test'),
    ('call_2', 101, 'test@example.com', '+919876543210', 'Test User', GETUTCDATE(), 'Test');

-- Run stored procedure
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 10;

-- Expected: Should return the IRF 102 entry (0 attempts for that IRF)
-- Should NOT return IRF 101 entry (2 attempts already)
```

### Test Case 2: Verify Call Attempt Counting

```sql
-- Check call attempts per candidate per IRF
SELECT 
    RecipientEmail,
    IRFId,
    COUNT(*) AS CallAttempts,
    MAX(CreatedAt) AS LastCall
FROM VapiCallLogs
WHERE RecipientEmail = 'test@example.com'
GROUP BY RecipientEmail, IRFId
ORDER BY IRFId;

-- Should show separate counts for each IRF
```

### Test Case 3: Verify Booking Status Check

```sql
-- Simulate successful booking for IRF 101
UPDATE VapiCallLogs
SET InterviewBooked = 1
WHERE RecipientEmail = 'test@example.com'
AND IRFId = 101
AND VapiCallId = 'call_1';

-- Run stored procedure
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 10;

-- Expected: IRF 101 should NOT appear (successfully booked)
-- Expected: IRF 102 should still appear (no booking yet)
```

---

## Migration Impact

### Existing Records

After running the migration script:
- **Records with JSON**: IRFId will be backfilled from `BookingConfirmationDetails` JSON
- **Records without JSON**: IRFId will remain NULL (old records before IRFId tracking)

### New Records

All new call logs will have IRFId populated:
- From controller: Parsed from irfId parameter
- From timer function: Taken from candidate.IRFId
- From webhook: Parsed from tool call arguments

---

## Monitoring Queries (Updated)

### Call Attempts Per Candidate Per IRF

```sql
-- See how many times each candidate was called for each IRF
SELECT 
    c.Name AS CandidateName,
    c.EmailId,
    vcl.IRFId,
    irf.Name AS IRFName,
    COUNT(*) AS CallAttempts,
    MAX(vcl.CreatedAt) AS LastCallDate,
    MAX(CASE WHEN vcl.InterviewBooked = 1 THEN 'Yes' ELSE 'No' END) AS SuccessfulBooking
FROM VapiCallLogs vcl
LEFT JOIN Candidate c ON vcl.RecipientEmail = c.EmailId AND vcl.IRFId = c.IRFId
LEFT JOIN IRF irf ON vcl.IRFId = irf.id
GROUP BY c.Name, c.EmailId, vcl.IRFId, irf.Name
ORDER BY CallAttempts DESC, LastCallDate DESC;
```

### Candidates Eligible for Retry (Per IRF)

```sql
-- Find candidates who were called once per IRF but didn't book
SELECT 
    vcl.RecipientEmail,
    vcl.RecipientName,
    vcl.IRFId,
    irf.Name AS IRFName,
    COUNT(*) AS CallAttempts,
    MAX(vcl.InterviewBooked) AS HasBooking
FROM VapiCallLogs vcl
LEFT JOIN IRF irf ON vcl.IRFId = irf.id
GROUP BY vcl.RecipientEmail, vcl.RecipientName, vcl.IRFId, irf.Name
HAVING COUNT(*) = 1 
    AND (MAX(CAST(ISNULL(vcl.InterviewBooked, 0) AS INT)) = 0)
ORDER BY vcl.IRFId, vcl.RecipientEmail;
```

### IRF-wise Call Statistics

```sql
-- Performance by IRF
SELECT 
    irf.id AS IRFId,
    irf.Name AS IRFName,
    COUNT(*) AS TotalCalls,
    SUM(CASE WHEN vcl.InterviewBooked = 1 THEN 1 ELSE 0 END) AS SuccessfulBookings,
    COUNT(DISTINCT vcl.RecipientEmail) AS UniqueCandidates,
    (SUM(CASE WHEN vcl.InterviewBooked = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) AS SuccessRate
FROM VapiCallLogs vcl
INNER JOIN IRF irf ON vcl.IRFId = irf.id
WHERE vcl.IRFId IS NOT NULL
GROUP BY irf.id, irf.Name
ORDER BY TotalCalls DESC;
```

---

## Breaking Changes

### ⚠️ Important Notes

1. **Existing VapiCallLogs records**: Will have NULL for IRFId unless backfilled
2. **Backward compatibility**: Code handles NULL IRFId gracefully
3. **No data loss**: Existing records remain intact
4. **Stored procedure**: Must be re-deployed after database migration

---

## Deployment Checklist

- [ ] **Step 1**: Backup VapiCallLogs table
- [ ] **Step 2**: Run migration script `Add_IRFId_To_VapiCallLogs.sql`
- [ ] **Step 3**: Verify column and indexes were created
- [ ] **Step 4**: Re-run stored procedure script `usp_GetEligibleCandidatesForVapiCalls.sql`
- [ ] **Step 5**: Test stored procedure with sample data
- [ ] **Step 6**: Deploy updated code to production
- [ ] **Step 7**: Monitor first few timer function runs
- [ ] **Step 8**: Verify call attempts are tracked per IRF

---

## Rollback Plan

If you need to rollback (not recommended unless critical issue):

```sql
-- Remove indexes
DROP INDEX IX_VapiCallLogs_RecipientEmail_IRFId ON VapiCallLogs;
DROP INDEX IX_VapiCallLogs_IRFId ON VapiCallLogs;

-- Remove column
ALTER TABLE VapiCallLogs DROP COLUMN IRFId;

-- Revert stored procedure to old version (group by RecipientEmail only)
```

**Note**: You'd also need to redeploy old code version.

---

## Verification Queries

### 1. Check Migration Success

```sql
-- Should return 1 row showing IRFId column
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'VapiCallLogs'
AND COLUMN_NAME = 'IRFId';
```

### 2. Check Index Creation

```sql
-- Should return 2 rows (both indexes)
SELECT name, type_desc
FROM sys.indexes
WHERE object_id = OBJECT_ID('VapiCallLogs')
AND name LIKE '%IRFId%';
```

### 3. Verify Backfill Results

```sql
-- See how many records have IRFId populated
SELECT 
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN IRFId IS NOT NULL THEN 1 ELSE 0 END) AS WithIRFId,
    SUM(CASE WHEN IRFId IS NULL THEN 1 ELSE 0 END) AS WithoutIRFId,
    (SUM(CASE WHEN IRFId IS NOT NULL THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) AS PercentagePopulated
FROM VapiCallLogs;
```

### 4. Test Stored Procedure Logic

```sql
-- Test with a known scenario
-- Insert test data first, then run stored procedure
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 10;

-- Verify results match expected eligibility
```

---

## Examples with Real Data

### Example 1: Candidate in 2 IRFs, Never Called

**Data**:
```
Candidate: jane@example.com
- IRF 101: Java Developer (0 calls)
- IRF 102: Python Developer (0 calls)
```

**Stored Procedure Result**:
```
IRFId: 101, Email: jane@example.com, CallAttempts: 0 ✅ Eligible
IRFId: 102, Email: jane@example.com, CallAttempts: 0 ✅ Eligible
```

**Outcome**: Both IRF entries eligible, will call for first available

### Example 2: Candidate in 2 IRFs, Called Twice for One

**Data**:
```
Candidate: bob@example.com
- IRF 101: Java Developer (2 calls, not booked)
- IRF 102: Python Developer (0 calls)
```

**VapiCallLogs**:
```
Email: bob@example.com, IRFId: 101, CallAttempts: 2
Email: bob@example.com, IRFId: 102, CallAttempts: 0
```

**Stored Procedure Result**:
```
IRFId: 101, Email: bob@example.com, CallAttempts: 2 ❌ Not Eligible (max reached)
IRFId: 102, Email: bob@example.com, CallAttempts: 0 ✅ Eligible
```

**Outcome**: Only IRF 102 entry eligible

### Example 3: Candidate in 2 IRFs, Successfully Booked One

**Data**:
```
Candidate: alice@example.com
- IRF 101: Java Developer (1 call, booked successfully)
- IRF 102: Python Developer (0 calls)
```

**VapiCallLogs**:
```
Email: alice@example.com, IRFId: 101, CallAttempts: 1, InterviewBooked: 1
Email: alice@example.com, IRFId: 102, CallAttempts: 0
```

**Stored Procedure Result**:
```
IRFId: 101, Email: alice@example.com ❌ Not Eligible (already booked)
IRFId: 102, Email: alice@example.com ✅ Eligible (no booking yet)
```

**Outcome**: Only IRF 102 entry eligible

---

## Performance Impact

### Index Benefits

**Before**: Full table scan on VapiCallLogs when checking attempts

**After**: 
- `IX_VapiCallLogs_IRFId`: Fast filtering by IRF
- `IX_VapiCallLogs_RecipientEmail_IRFId`: Optimized for eligibility CTE

**Expected Improvement**: 10-50x faster eligibility queries (depends on data volume)

---

## Business Logic Summary

### Call Attempt Limits (Per Candidate Per IRF)

| Scenario | Call Attempts | Can Call Again? |
|----------|---------------|-----------------|
| Never called for this IRF | 0 | ✅ YES |
| Called once, not booked | 1 | ✅ YES (retry) |
| Called once, booked successfully | 1 | ❌ NO (mission accomplished) |
| Called twice, not booked | 2 | ❌ NO (max attempts reached) |
| Called twice, booked on 2nd attempt | 2 | ❌ NO (mission accomplished) |

### Key Points

1. **Per IRF Tracking**: Each IRF has independent attempt counter
2. **2 Attempts Max**: Per candidate per IRF combination
3. **Success Stops Calls**: Successful booking prevents further calls for that IRF
4. **Cross-IRF Independence**: Attempts for IRF A don't affect eligibility for IRF B

---

## Summary

✅ **IRFId column added** to VapiCallLogs  
✅ **Stored procedure updated** to group by email AND IRFId  
✅ **Entity model updated** with IRFId property  
✅ **Repository updated** to save and update IRFId  
✅ **Controller updated** to extract and save IRFId  
✅ **Timer function updated** to pass IRFId  
✅ **Indexes created** for optimal performance  
✅ **Migration script provided** with backfill logic  
✅ **Documentation updated** with examples  

**Impact**: Candidates can now be called up to 2 times per IRF, not globally. This is the correct business logic.

---

**Migration Date**: February 24, 2026  
**Status**: ✅ Ready for Deployment  
**Breaking Changes**: None (backward compatible with NULL IRFId)  
