CREATE OR ALTER PROCEDURE [dbo].[usp_FetchChallanInterviewers]
    @ChallanId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        tci.Userid,
        (ul.FirstName + ' ' + ul.LastName) AS FullName,
        tci.PAN,
        ISNULL(tic.IsPanVerified, 0)        AS IsPanVerified,
        ISNULL(tic.NameAtPan, '')           AS NameAtPan,
        tci.AmountPaid,
        tci.TdsAmount
    FROM TdsChallanInterviewer tci
    JOIN UserList ul ON ul.id = tci.Userid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = tci.Userid
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE tci.ChallanId = @ChallanId;
END
GO
