CREATE OR ALTER PROCEDURE [dbo].[usp_FindNaukriProspectByOnboardedUserId]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id
    FROM NaukriProspectiveInterviewer
    WHERE OnboardedUserId = @UserId
    ORDER BY Id DESC;
END
GO
