-- =============================================
-- Migration: Add IRFId column to VapiCallLogs table
-- Date: 2026-02-24
-- Description: Adds IRFId column to track call attempts per candidate per IRF
-- =============================================

-- Check if column already exists
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'IRFId'
    AND Object_ID = Object_ID(N'VapiCallLogs'))
BEGIN
    PRINT 'Adding IRFId column to VapiCallLogs table...';
    
    -- Add the column (nullable to allow existing records)
    ALTER TABLE VapiCallLogs
    ADD IRFId INT NULL;
    
    PRINT 'IRFId column added successfully.';
    
    -- Create index on IRFId for performance
    CREATE NONCLUSTERED INDEX [IX_VapiCallLogs_IRFId] 
    ON VapiCallLogs([IRFId]);
    
    PRINT 'Index IX_VapiCallLogs_IRFId created.';
    
    -- Create composite index on RecipientEmail and IRFId for eligibility queries
    CREATE NONCLUSTERED INDEX [IX_VapiCallLogs_RecipientEmail_IRFId] 
    ON VapiCallLogs([RecipientEmail], [IRFId]);
    
    PRINT 'Index IX_VapiCallLogs_RecipientEmail_IRFId created.';
    
    PRINT 'Migration completed successfully!';
END
ELSE
BEGIN
    PRINT 'IRFId column already exists in VapiCallLogs table. No changes made.';
END
GO

-- Optional: Attempt to backfill IRFId for existing records if possible
-- This tries to extract IRFId from BookingConfirmationDetails JSON
-- Only runs if there are records without IRFId

IF EXISTS (SELECT 1 FROM VapiCallLogs WHERE IRFId IS NULL)
BEGIN
    PRINT 'Attempting to backfill IRFId from BookingConfirmationDetails...';
    
    UPDATE VapiCallLogs
    SET IRFId = CAST(JSON_VALUE(BookingConfirmationDetails, '$.irfId') AS INT)
    WHERE IRFId IS NULL
    AND BookingConfirmationDetails IS NOT NULL
    AND ISJSON(BookingConfirmationDetails) = 1
    AND JSON_VALUE(BookingConfirmationDetails, '$.irfId') IS NOT NULL;
    
    PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' records backfilled with IRFId from BookingConfirmationDetails.';
END

-- Show summary
SELECT 
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN IRFId IS NOT NULL THEN 1 ELSE 0 END) AS RecordsWithIRFId,
    SUM(CASE WHEN IRFId IS NULL THEN 1 ELSE 0 END) AS RecordsWithoutIRFId
FROM VapiCallLogs;

PRINT 'Migration summary displayed.';
GO
