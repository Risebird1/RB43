CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerEmailAvailabilityHistory]
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
    FROM   dbo.InterviewerEmailAvailabilityHistory h
    WHERE  h.RecordId = @RecordId
    ORDER  BY h.ChangedAt DESC;
END
GO
