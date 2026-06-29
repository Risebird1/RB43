-- Insert "Quarterly TDS" nav item under Finance for Finance + Admin profiles.
-- Run ONCE on each environment after deploying tables and SPs.
-- Safe to re-run (INSERT only if not exists).

DECLARE @FunctionId INT;

-- Insert nav function if it does not exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[Function] WHERE sUrl = '/Tds/QuarterlyTds')
BEGIN
    INSERT INTO [dbo].[Function] (functionName, sUrl, idParentFunction, iDisplayOrder)
    VALUES ('Quarterly TDS', '/Tds/QuarterlyTds', NULL, 12);
    SET @FunctionId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @FunctionId = id FROM [dbo].[Function] WHERE sUrl = '/Tds/QuarterlyTds';
END

-- Link to Finance profile
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ProfileFunction] pf
    JOIN [dbo].[Profile] p ON p.id = pf.idProfile
    WHERE p.Name = 'Finance' AND pf.idFunction = @FunctionId
)
BEGIN
    INSERT INTO [dbo].[ProfileFunction] (idProfile, idFunction)
    SELECT p.id, @FunctionId FROM [dbo].[Profile] p WHERE p.Name = 'Finance';
END

-- Link to Admin profile
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ProfileFunction] pf
    JOIN [dbo].[Profile] p ON p.id = pf.idProfile
    WHERE p.Name = 'Admin' AND pf.idFunction = @FunctionId
)
BEGIN
    INSERT INTO [dbo].[ProfileFunction] (idProfile, idFunction)
    SELECT p.id, @FunctionId FROM [dbo].[Profile] p WHERE p.Name = 'Admin';
END

PRINT 'Quarterly TDS nav entry inserted/verified for Finance and Admin profiles.';
