-- Migration script to update VapiCallLogs table
-- Removes foreign key constraint and renames columns from Candidate* to Recipient*

-- Step 1: Drop the foreign key constraint
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_VapiCallLogs_Interview')
BEGIN
    ALTER TABLE [dbo].[VapiCallLogs] DROP CONSTRAINT [FK_VapiCallLogs_Interview];
    PRINT 'Foreign key constraint FK_VapiCallLogs_Interview dropped successfully.';
END
GO

-- Step 2: Rename columns using sp_rename
-- Rename CandidateId to RecipientId
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VapiCallLogs') AND name = 'CandidateId')
BEGIN
    EXEC sp_rename 'dbo.VapiCallLogs.CandidateId', 'RecipientId', 'COLUMN';
    PRINT 'Column CandidateId renamed to RecipientId.';
END
GO

-- Rename CandidatePhone to RecipientPhone
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VapiCallLogs') AND name = 'CandidatePhone')
BEGIN
    EXEC sp_rename 'dbo.VapiCallLogs.CandidatePhone', 'RecipientPhone', 'COLUMN';
    PRINT 'Column CandidatePhone renamed to RecipientPhone.';
END
GO

-- Rename CandidateName to RecipientName
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VapiCallLogs') AND name = 'CandidateName')
BEGIN
    EXEC sp_rename 'dbo.VapiCallLogs.CandidateName', 'RecipientName', 'COLUMN';
    PRINT 'Column CandidateName renamed to RecipientName.';
END
GO

-- Rename CandidateEmail to RecipientEmail
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VapiCallLogs') AND name = 'CandidateEmail')
BEGIN
    EXEC sp_rename 'dbo.VapiCallLogs.CandidateEmail', 'RecipientEmail', 'COLUMN';
    PRINT 'Column CandidateEmail renamed to RecipientEmail.';
END
GO

-- Step 3: Drop and recreate index on CandidateId (now RecipientId)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VapiCallLogs_CandidateId' AND object_id = OBJECT_ID('dbo.VapiCallLogs'))
BEGIN
    DROP INDEX [IX_VapiCallLogs_CandidateId] ON [dbo].[VapiCallLogs];
    PRINT 'Index IX_VapiCallLogs_CandidateId dropped.';
END
GO

-- Create new index on RecipientId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VapiCallLogs_RecipientId' AND object_id = OBJECT_ID('dbo.VapiCallLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VapiCallLogs_RecipientId] ON [dbo].[VapiCallLogs]([RecipientId]);
    PRINT 'Index IX_VapiCallLogs_RecipientId created successfully.';
END
GO

PRINT 'Migration completed successfully!';
PRINT 'Changes made:';
PRINT '  - Removed FK_VapiCallLogs_Interview foreign key constraint';
PRINT '  - Renamed CandidateId to RecipientId';
PRINT '  - Renamed CandidatePhone to RecipientPhone';
PRINT '  - Renamed CandidateName to RecipientName';
PRINT '  - Renamed CandidateEmail to RecipientEmail';
PRINT '  - Updated indexes accordingly';
PRINT '';
PRINT '========================================';
PRINT 'IMPORTANT: NEXT STEP REQUIRED';
PRINT '========================================';
PRINT 'You must also update the stored procedures that reference VapiCallLogs.';
PRINT '';
PRINT 'Option 1 (Recommended): Run the combined migration script:';
PRINT '  Execute: Risebird.DB\StoredProcedures\VapiCallLogs_StoredProcedures_Migration.sql';
PRINT '';
PRINT 'Option 2: Update individual stored procedures:';
PRINT '  1. Execute: Risebird.DB\StoredProcedures\usp_GetVapiCallStatistics.sql';
PRINT '  2. Execute: Risebird.DB\StoredProcedures\usp_GetPendingCandidatesForVapiCalls.sql';
PRINT '';
PRINT 'Affected stored procedures:';
PRINT '  - usp_GetVapiCallStatistics (uses RecipientName, RecipientPhone)';
PRINT '  - usp_GetPendingCandidatesForVapiCalls (uses RecipientId in JOIN)';
PRINT '========================================';
GO
