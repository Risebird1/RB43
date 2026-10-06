-- Seed Operation -> Logs menu for Admin / SupportAdmin / SeniorHR / HR profiles.
-- Safe to re-run: skips inserts when FunctionName already exists.

SET NOCOUNT ON;

DECLARE @operationId INT;
DECLARE @logsId INT;

IF NOT EXISTS (SELECT 1 FROM dbo.[Function] WHERE FunctionName = 'Operation' AND ISNULL(sURL, '') = '')
BEGIN
    INSERT INTO dbo.[Function] (FunctionName, Description, idParentFunction, sIcon, sURL, iDisplayOrder, IsCommon)
    VALUES ('Operation', 'Operations', NULL, 'fas fa-clipboard-list', NULL, 90, 1);
END

SELECT @operationId = id
FROM dbo.[Function]
WHERE FunctionName = 'Operation' AND ISNULL(sURL, '') = '';

IF @operationId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.[Function] WHERE FunctionName = 'Logs' AND sURL = '/ApplicationLogs')
BEGIN
    INSERT INTO dbo.[Function] (FunctionName, Description, idParentFunction, sIcon, sURL, iDisplayOrder, IsCommon)
    VALUES ('Logs', 'Application exception and diagnostic logs', @operationId, 'fas fa-bug', '/ApplicationLogs', 1, 1);
END

SELECT @logsId = id
FROM dbo.[Function]
WHERE FunctionName = 'Logs' AND sURL = '/ApplicationLogs';

-- Grant to internal ops profiles
DECLARE @profileIds TABLE (id INT);
INSERT INTO @profileIds (id)
SELECT id FROM dbo.[Profile]
WHERE [Name] IN ('Admin');

IF @operationId IS NOT NULL
BEGIN
    INSERT INTO dbo.ProfileFunction (idprofile, idfunction, dAccess)
    SELECT p.id, @operationId, 1
    FROM @profileIds p
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.ProfileFunction pf
        WHERE pf.idprofile = p.id AND pf.idfunction = @operationId
    );
END

IF @logsId IS NOT NULL
BEGIN
    INSERT INTO dbo.ProfileFunction (idprofile, idfunction, dAccess)
    SELECT p.id, @logsId, 1
    FROM @profileIds p
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.ProfileFunction pf
        WHERE pf.idprofile = p.id AND pf.idfunction = @logsId
    );
END

PRINT 'Operation -> Logs menu seed complete.';
PRINT 'Operation Function id: ' + ISNULL(CAST(@operationId AS VARCHAR(20)), 'N/A');
PRINT 'Logs Function id: ' + ISNULL(CAST(@logsId AS VARCHAR(20)), 'N/A');
GO
