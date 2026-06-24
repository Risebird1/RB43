-- Resolves the active UserList id (and its InterviewerProfile id, if any) for an email.
CREATE OR ALTER PROCEDURE [dbo].[usp_FindActiveUserByEmail]
    @Email VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1
        ul.id AS UserId,
        ip.Id AS ProfileId
    FROM UserList ul
    LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id
    WHERE LOWER(LTRIM(RTRIM(ul.EmailId))) = LOWER(LTRIM(RTRIM(@Email)))
      AND ul.IsActive = 1;
END
GO
