-- Removes a MAG from today's (IST) active priority list. Returns rows affected.
CREATE OR ALTER PROCEDURE [dbo].[usp_ClearNaukriPriorityMag]
    @AssessmentGroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);

    UPDATE NaukriPriorityMag
    SET IsActive = 0, ClearedAt = GETUTCDATE()
    WHERE PriorityDate = @Today AND IsActive = 1 AND AssessmentGroupId = @AssessmentGroupId;

    SELECT @@ROWCOUNT;
END
GO
