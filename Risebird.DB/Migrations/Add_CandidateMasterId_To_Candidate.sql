-- =============================================
-- Migration: Add CandidateMasterId column to Candidate table
-- Date: 2026-02-24
-- Description: Adds CandidateMasterId to enable three-way relationship with CandidatesMaster and UserList
-- =============================================

-- Check if column already exists
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'CandidateMasterId'
    AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    PRINT 'Adding CandidateMasterId column to Candidate table...';
    
    -- Add the column (nullable to allow existing records)
    ALTER TABLE Candidate
    ADD CandidateMasterId INT NULL;
    
    PRINT 'CandidateMasterId column added successfully.';
    
    -- Create index for performance
    CREATE NONCLUSTERED INDEX [IX_Candidate_CandidateMasterId] 
    ON Candidate([CandidateMasterId]);
    
    PRINT 'Index IX_Candidate_CandidateMasterId created.';
    
    -- Optional: Add foreign key constraint
    -- Uncomment if you want to enforce referential integrity
    /*
    ALTER TABLE Candidate
    ADD CONSTRAINT FK_Candidate_CandidatesMaster 
    FOREIGN KEY (CandidateMasterId) REFERENCES CandidatesMaster(id);
    
    PRINT 'Foreign key constraint added.';
    */
    
    PRINT 'Migration completed successfully!';
END
ELSE
BEGIN
    PRINT 'CandidateMasterId column already exists in Candidate table. No changes made.';
END
GO

-- Show summary
SELECT 
    COUNT(*) AS TotalCandidates,
    SUM(CASE WHEN CandidateMasterId IS NOT NULL THEN 1 ELSE 0 END) AS WithCandidateMasterId,
    SUM(CASE WHEN CandidateMasterId IS NULL THEN 1 ELSE 0 END) AS WithoutCandidateMasterId,
    (SUM(CASE WHEN CandidateMasterId IS NOT NULL THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) AS PercentagePopulated
FROM Candidate;

PRINT 'Migration summary displayed.';
GO
