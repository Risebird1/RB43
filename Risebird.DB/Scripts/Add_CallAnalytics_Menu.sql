/*
    Adds "Call Analytics" (/HR/CallAnalytics) to the left sidebar menu.

    The sidebar is built from dbo.[Function] + dbo.ProfileFunction (sp_GetMenuBasedOnEmailId).
    The menu item is granted to the same roles the page itself allows (HRController.CallAnalytics):
    Admin, HR, SeniorHR, SupportAdmin, SeniorInterviewerManagement, InterviewerManagement.

    Placement: copied from the existing Call Recordings / Call Catalogue menu row when one exists,
    so the new link sits right after it; otherwise it is added as a top-level item.

    Safe to run more than once. Users see the link after their next login (the menu is cached in session).
*/
SET NOCOUNT ON;

BEGIN TRAN;

DECLARE @functionId INT = (SELECT TOP 1 id FROM dbo.[Function] WHERE sURL = '/HR/CallAnalytics');

IF @functionId IS NULL
BEGIN
    DECLARE @parentId INT, @displayOrder INT;

    -- Sit next to an existing call menu item if there is one
    SELECT TOP 1 @parentId = idParentFunction, @displayOrder = iDisplayOrder + 1
    FROM dbo.[Function]
    WHERE sURL LIKE '/HR/CallRecordingsDetails%' OR sURL LIKE '/HR/CallCatalogue%'
    ORDER BY iDisplayOrder;

    -- Otherwise: top level, same parent as another top-level HR item, at the end of the list
    IF @displayOrder IS NULL
    BEGIN
        SELECT TOP 1 @parentId = idParentFunction
        FROM dbo.[Function]
        WHERE sURL = '/HR/MyInterviews';

        SELECT @displayOrder = ISNULL(MAX(iDisplayOrder), 0) + 1 FROM dbo.[Function];
    END

    INSERT INTO dbo.[Function] (FunctionName, [Description], idParentFunction, sIcon, sURL, iDisplayOrder)
    VALUES ('Call Analytics', 'Calls made by each Risebird user, with recordings', ISNULL(@parentId, 0),
            'fas fa-chart-bar', '/HR/CallAnalytics', @displayOrder);

    SET @functionId = SCOPE_IDENTITY();
END

-- Grant the menu item to the roles that can open the page
INSERT INTO dbo.ProfileFunction (idProfile, idFunction, dAccess)
SELECT p.id, @functionId, 1
FROM dbo.[Profile] p
WHERE p.[Name] IN ('Admin', 'HR', 'SeniorHR', 'SupportAdmin', 'SeniorInterviewerManagement', 'InterviewerManagement')
  AND NOT EXISTS (SELECT 1 FROM dbo.ProfileFunction pf WHERE pf.idProfile = p.id AND pf.idFunction = @functionId);

COMMIT TRAN;

-- Check
SELECT f.id, f.FunctionName, f.sURL, f.idParentFunction, f.iDisplayOrder, p.[Name] AS ProfileName
FROM dbo.[Function] f
JOIN dbo.ProfileFunction pf ON pf.idFunction = f.id
JOIN dbo.[Profile] p ON p.id = pf.idProfile
WHERE f.sURL = '/HR/CallAnalytics';
GO
