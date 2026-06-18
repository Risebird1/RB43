CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerProfileIdByUserId]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id
    FROM InterviewerProfile
    WHERE UserId = @UserId;
END
GO
