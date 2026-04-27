CREATE OR ALTER PROCEDURE [dbo].[usp_InsertNaukriOnboardingHistory]
    @RecordId   INT,
    @EventType  VARCHAR(50),
    @FromValue  NVARCHAR(500) = NULL,
    @ToValue    NVARCHAR(500) = NULL,
    @ChangedBy  VARCHAR(150)  = 'System'
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.NaukriOnboardingHistory
        (RecordId, EventType, FromValue, ToValue, ChangedBy, ChangedAt)
    VALUES
        (@RecordId, @EventType, @FromValue, @ToValue, @ChangedBy, GETUTCDATE());
END
GO
