IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_InsertBatchCallLog')
    DROP PROCEDURE usp_InsertBatchCallLog
GO

CREATE PROCEDURE [dbo].[usp_InsertBatchCallLog]
    @MagName        NVARCHAR(200),
    @InitiatedBy    VARCHAR(100),
    @TotalProspects INT,
    @NewId          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO NaukriBatchCallLog (MagName, InitiatedBy, InitiatedAt, ProspectsAttempted, Status)
    VALUES (@MagName, @InitiatedBy, GETUTCDATE(), @TotalProspects, 'Running')

    SET @NewId = SCOPE_IDENTITY()
END
GO
