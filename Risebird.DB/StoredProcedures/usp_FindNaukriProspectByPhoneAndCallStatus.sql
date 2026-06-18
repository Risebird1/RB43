CREATE OR ALTER PROCEDURE [dbo].[usp_FindNaukriProspectByPhoneAndCallStatus]
    @Phone  VARCHAR(50),
    @Status VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id
    FROM NaukriProspectiveInterviewer
    WHERE ParsedPhone = @Phone
      AND PlivoCallStatus = @Status
    ORDER BY Id DESC;
END
GO
