CREATE OR ALTER PROCEDURE [dbo].[usp_FetchChallanInterviewers]
    @ChallanId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        tci.Userid,
        (ul.FirstName + ' ' + ul.LastName) AS FullName,
        tci.PAN,
        tci.AmountPaid,
        tci.TdsAmount
    FROM TdsChallanInterviewer tci
    JOIN UserList ul ON ul.id = tci.Userid
    WHERE tci.ChallanId = @ChallanId;
END
GO
