CREATE OR ALTER PROCEDURE [dbo].[usp_GetNaukriOnboardingHistory]
    @RecordId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.[Id],
        h.[RecordId],
        h.[EventType],
        h.[FromValue],
        h.[ToValue],
        h.[ChangedBy],
        h.[ChangedAt]
    FROM   dbo.NaukriOnboardingHistory h
    WHERE  h.RecordId = @RecordId
    ORDER  BY h.ChangedAt DESC;
END
GO
