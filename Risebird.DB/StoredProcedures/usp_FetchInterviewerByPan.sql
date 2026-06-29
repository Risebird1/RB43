CREATE OR ALTER PROCEDURE [dbo].[usp_FetchInterviewerByPan]
    @PAN NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1
        ul.id        AS Userid,
        ul.EmailId   AS Email,
        ul.FirstName,
        ul.LastName,
        tic.PAN
    FROM TransInterviewerAccDetails tic
    JOIN InterviewerProfile irp ON irp.id = tic.InterviewerProfileId
    JOIN UserList ul ON ul.id = irp.UserId
    WHERE tic.PAN = @PAN;
END
GO
