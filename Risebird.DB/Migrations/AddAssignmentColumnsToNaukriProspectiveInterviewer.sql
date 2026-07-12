-- ============================================================================================
-- Migration: Ticket-style assignment for NaukriOnboarding prospects
--
-- Adds AssignedToUserId / AssignedToEmail / AssignedToName / AssignedAt (+ supporting index) to
-- NaukriProspectiveInterviewer, then backfills every currently-unassigned prospect with a randomly
-- selected active InterviewerManagement / SeniorInterviewerManagement operator, so each existing
-- "ticket" has an accountable owner.
--
-- Idempotent: safe to re-run. Columns/index are added only if missing; the backfill touches only
-- rows that are still unassigned.
-- ============================================================================================
SET NOCOUNT ON;

-------------------------------------------------------------------------------------------------
-- 1) Schema: add columns
-------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'AssignedToUserId' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedToUserId] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'AssignedToEmail' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedToEmail] VARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'AssignedToName' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedToName] NVARCHAR(200) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'AssignedAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedAt] DATETIME NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NaukriProspectiveInterviewer_AssignedToUserId' AND object_id = OBJECT_ID(N'NaukriProspectiveInterviewer'))
    CREATE NONCLUSTERED INDEX [IX_NaukriProspectiveInterviewer_AssignedToUserId]
        ON [dbo].[NaukriProspectiveInterviewer] ([AssignedToUserId]);
GO

-------------------------------------------------------------------------------------------------
-- 2) Backfill: give every unassigned prospect a random eligible operator.
--    NEWID() inside CROSS APPLY is evaluated per-row, so ownership is spread across operators
--    rather than everyone landing on the same person.
-------------------------------------------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM dbo.UserList ul
    INNER JOIN dbo.Profile pf ON ul.idProfile = pf.id
    WHERE pf.[Name] IN ('InterviewerManagement', 'SeniorInterviewerManagement')
      AND ul.IsActive = 1
)
BEGIN
    UPDATE np
    SET np.AssignedToUserId = a.id,
        np.AssignedToEmail  = a.EmailId,
        np.AssignedToName   = a.FullName,
        np.AssignedAt       = GETUTCDATE(),
        np.UpdatedAt        = GETUTCDATE()
    FROM dbo.NaukriProspectiveInterviewer np
    CROSS APPLY (
        SELECT TOP 1
            ul.id,
            ul.EmailId,
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS FullName
        FROM dbo.UserList ul
        INNER JOIN dbo.Profile pf ON ul.idProfile = pf.id
        WHERE pf.[Name] IN ('InterviewerManagement', 'SeniorInterviewerManagement')
          AND ul.IsActive = 1
        ORDER BY NEWID()
    ) a
    WHERE np.AssignedToUserId IS NULL;

    PRINT 'Backfill complete. Rows assigned: ' + CAST(@@ROWCOUNT AS VARCHAR(20));
END
ELSE
BEGIN
    PRINT 'No active InterviewerManagement / SeniorInterviewerManagement operators found; backfill skipped.';
END
GO

-------------------------------------------------------------------------------------------------
-- 3) Self-verification diagnostics
-------------------------------------------------------------------------------------------------
SELECT
    COUNT(*)                                                            AS TotalProspects,
    SUM(CASE WHEN AssignedToUserId IS NOT NULL THEN 1 ELSE 0 END)       AS Assigned,
    SUM(CASE WHEN AssignedToUserId IS NULL     THEN 1 ELSE 0 END)       AS StillUnassigned
FROM dbo.NaukriProspectiveInterviewer;

SELECT AssignedToEmail, COUNT(*) AS Tickets
FROM dbo.NaukriProspectiveInterviewer
WHERE AssignedToUserId IS NOT NULL
GROUP BY AssignedToEmail
ORDER BY Tickets DESC;
GO
