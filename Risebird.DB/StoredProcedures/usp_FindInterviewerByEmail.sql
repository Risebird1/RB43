-- Resolves the UserList id (and its InterviewerProfile id, if any) for an email REGARDLESS of
-- active status, and also reports whether that account is currently active.
--
-- This differs from usp_FindActiveUserByEmail (which filters IsActive = 1): the Naukri onboarding
-- "adopt existing interviewer" path needs to find accounts that exist but are INACTIVE (e.g. a prior
-- MAG-direct add that was never activated), so that onboarding can complete instead of dead-ending.
CREATE OR ALTER PROCEDURE [dbo].[usp_FindInterviewerByEmail]
    @Email VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        ul.id      AS UserId,
        ip.Id      AS ProfileId,
        ul.IsActive AS IsActive
    FROM UserList ul
    LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id
    WHERE LOWER(LTRIM(RTRIM(ul.EmailId))) = LOWER(LTRIM(RTRIM(@Email)))
    -- Prefer a row that actually has an interviewer profile, then the most recent account.
    ORDER BY CASE WHEN ip.Id IS NOT NULL THEN 0 ELSE 1 END, ul.id DESC;
END
GO
