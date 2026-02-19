# VapiCallLogs Table Column Rename Summary

## Overview
This document summarizes the changes made to rename columns in the VapiCallLogs table from "Candidate*" to more generic "Recipient*" names, and to remove the foreign key constraint on the InterviewId column.

## Changes Made

### 1. Database Schema Changes

#### File: `Risebird.DB\Tables\VapiCallLogs.sql`
- **Removed**: Foreign key constraint `FK_VapiCallLogs_Interview`
- **Renamed Columns**:
  - `CandidateId` → `RecipientId`
  - `CandidatePhone` → `RecipientPhone`
  - `CandidateName` → `RecipientName`
  - `CandidateEmail` → `RecipientEmail`
- **Updated Index**: `IX_VapiCallLogs_CandidateId` → `IX_VapiCallLogs_RecipientId`

#### File: `Risebird.DB\Tables\VapiCallLogs_Migration.sql` (NEW)
- Created migration script to update existing databases
- Safely drops foreign key constraint
- Renames all columns using sp_rename
- Updates indexes accordingly
- Includes existence checks to prevent errors if already migrated

### 2. Entity Model Changes

#### File: `Risebird.Entities\Data\VapiCallLog.cs`
Updated property names:
```csharp
- public int? CandidateId { get; set; }
- public string CandidatePhone { get; set; }
- public string CandidateName { get; set; }
- public string CandidateEmail { get; set; }

+ public int? RecipientId { get; set; }
+ public string RecipientPhone { get; set; }
+ public string RecipientName { get; set; }
+ public string RecipientEmail { get; set; }
```

### 3. Repository Changes

#### File: `Risebird.Repository\VapiCallRepository.cs`
- Updated INSERT SQL statement to use new column names
- Renamed method: `GetCallLogsByCandidateIdAsync` → `GetCallLogsByRecipientIdAsync`
- Updated SQL query to use `RecipientId` instead of `CandidateId`

#### File: `Risebird.Repository.Interface\IVapiCallRepository.cs`
- Updated interface method signature
- Updated XML documentation comments

### 4. Business Logic Changes

#### File: `Risebird.Business.Interface\IVapiInterviewService.cs`
- **Renamed Class**: `CandidateCallInfo` → `RecipientCallInfo`
- **Updated Properties**:
  - `CandidateId` → `RecipientId` (now nullable)
  - `CandidateName` → `RecipientName`
  - `CandidateEmail` → `RecipientEmail`
  - `CandidatePhone` → `RecipientPhone`
  - `InterviewId` → now nullable
- **Updated Method Signatures**:
  - `InitiateBatchCallsAsync(List<CandidateCallInfo> candidates)` → `InitiateBatchCallsAsync(List<RecipientCallInfo> recipients)`
- Updated XML documentation comments

#### File: `Risebird.Business\VapiInterviewService.cs`
- Updated `InitiateBatchCallsAsync` method to use new property names
- Updated variable names from `candidate` to `recipient`
- Updated `VapiCallLog` object initialization with new property names
- Updated XML documentation comments

### 5. Controller Changes

#### File: `Risebird.Web\Controllers\VapiInterviewCallController.cs`
- Updated `VapiCallLog` object initialization in call initiation endpoint
- Changed property assignments to use new names:
  - `CandidateId` → `RecipientId`
  - `CandidatePhone` → `RecipientPhone`
  - `CandidateName` → `RecipientName`
  - `CandidateEmail` → `RecipientEmail`

### 6. Stored Procedure Changes

#### File: `Risebird.DB\StoredProcedures\usp_GetVapiCallStatistics.sql`
- Updated to use `RecipientName` and `RecipientPhone` instead of `CandidateName` and `CandidatePhone`
- Modified the "Most recent calls" query to return recipient information

#### File: `Risebird.DB\StoredProcedures\usp_GetPendingCandidatesForVapiCalls.sql`
- Updated to use `RecipientId` instead of `CandidateId` in the subquery
- Updated the JOIN clause to use `vcl.RecipientId` instead of `vcl.CandidateId`
- Updated comments to reference "recipient" instead of "candidate"

## Migration Instructions

### For Existing Databases

1. **Backup your database** before running any migration scripts

2. Run the migration script:
   ```sql
   -- Execute the following file against your database
   Risebird.DB\Tables\VapiCallLogs_Migration.sql
   ```

3. Verify the changes:
   ```sql
   -- Check that columns have been renamed
   SELECT COLUMN_NAME 
   FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME = 'VapiCallLogs' 
   AND COLUMN_NAME LIKE 'Recipient%'
   
   -- Check that foreign key is removed
   SELECT name 
   FROM sys.foreign_keys 
   WHERE name = 'FK_VapiCallLogs_Interview'
   ```

### For New Databases

Simply execute the updated `VapiCallLogs.sql` table creation script.

### Update Stored Procedures

After running the table migration, update the stored procedures that reference the VapiCallLogs table.

**Option 1: Use the combined migration script (Recommended)**
```sql
-- Execute the combined migration script for all stored procedures
-- This will update both stored procedures at once
Risebird.DB\StoredProcedures\VapiCallLogs_StoredProcedures_Migration.sql
```

**Option 2: Update individual stored procedures**
```sql
-- 1. Update statistics stored procedure
-- Execute: Risebird.DB\StoredProcedures\usp_GetVapiCallStatistics.sql

-- 2. Update pending candidates stored procedure
-- Execute: Risebird.DB\StoredProcedures\usp_GetPendingCandidatesForVapiCalls.sql
```

## Impact Summary

### Breaking Changes
- Any existing code that directly queries the VapiCallLogs table using old column names will need to be updated
- Any stored procedures or views referencing the old column names must be updated
- The `InterviewId` column is now nullable without foreign key constraint, allowing calls to be made for non-interview purposes

### Backward Compatibility
- The migration script is safe to run multiple times
- The changes maintain the same data structure and relationships (except the removed FK constraint)
- All application code has been updated to use the new names

## Testing Recommendations

1. **Test the migration script** on a development database first
2. **Verify all Vapi call operations**:
   - Initiating calls
   - Saving call logs
   - Retrieving call logs by recipient
   - Retrieving call logs by interview
   - Updating call status
3. **Test stored procedures**:
   - Execute `usp_GetVapiCallStatistics` with various date ranges
   - Execute `usp_GetPendingCandidatesForVapiCalls` to verify JOIN logic works
   - Verify the output columns match the new naming convention
4. **Check that InterviewId can be null** for non-interview calls
5. **Verify that existing data** is preserved after migration

## Files Modified

### Database Files
- `Risebird.DB\Tables\VapiCallLogs.sql` (Updated table definition)
- `Risebird.DB\Tables\VapiCallLogs_Migration.sql` (NEW - Table migration script)
- `Risebird.DB\StoredProcedures\VapiCallLogs_StoredProcedures_Migration.sql` (NEW - Combined SP migration script)
- `Risebird.DB\StoredProcedures\usp_GetVapiCallStatistics.sql` (Updated)
- `Risebird.DB\StoredProcedures\usp_GetPendingCandidatesForVapiCalls.sql` (Updated)

### Application Files
- `Risebird.Entities\Data\VapiCallLog.cs`
- `Risebird.Repository\VapiCallRepository.cs`
- `Risebird.Repository.Interface\IVapiCallRepository.cs`
- `Risebird.Business\VapiInterviewService.cs`
- `Risebird.Business.Interface\IVapiInterviewService.cs`
- `Risebird.Web\Controllers\VapiInterviewCallController.cs`

## Build Status
✅ All changes compiled successfully with no errors
⚠️  Only standard warnings present (not related to these changes)

## Date
January 29, 2026
