IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetBatchCallHistory')
    DROP PROCEDURE usp_GetBatchCallHistory
GO

CREATE PROCEDURE [dbo].[usp_GetBatchCallHistory]
    @MagName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, MagName, InitiatedBy, InitiatedAt, ProspectsAttempted,
           ProspectsSucceeded, ProspectsFailed, ProspectsSkipped,
           CompletedAt, Status
    FROM NaukriBatchCallLog
    WHERE MagName = @MagName
    ORDER BY InitiatedAt DESC
END
GO
