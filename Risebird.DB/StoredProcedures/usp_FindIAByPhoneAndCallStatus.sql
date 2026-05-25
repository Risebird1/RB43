CREATE OR ALTER PROCEDURE [dbo].[usp_FindIAByPhoneAndCallStatus]
    @Phone       VARCHAR(50),
    @CallStatus  VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 Id
    FROM InterviewerEmailAvailability
    WHERE (Phone = @Phone OR Phone LIKE '%' + RIGHT(@Phone, 10))
      AND PlivoCallStatus = @CallStatus
    ORDER BY Id DESC;
END
GO
