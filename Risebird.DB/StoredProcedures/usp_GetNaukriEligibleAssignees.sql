IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriEligibleAssignees')
    DROP PROCEDURE usp_GetNaukriEligibleAssignees
GO

-- Returns the operators eligible to own a Naukri onboarding "ticket" (prospect):
-- active users whose profile is InterviewerManagement or SeniorInterviewerManagement.
CREATE PROCEDURE [dbo].[usp_GetNaukriEligibleAssignees]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ul.id                                                                             AS UserId,
        ul.EmailId                                                                        AS Email,
        NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS Name,
        pf.[Name]                                                                         AS ProfileName
    FROM dbo.UserList ul
    INNER JOIN dbo.Profile pf ON ul.idProfile = pf.id
    WHERE pf.[Name] IN ('InterviewerManagement', 'SeniorInterviewerManagement')
      AND ul.IsActive = 1
    ORDER BY Name, ul.EmailId;
END
GO
