-- Flags a MAG as priority for today's (IST) priority-intent-collection push.
-- Enforces the maximum number of simultaneously active priority MAGs (@MaxPriority, default 2).
-- Idempotent: re-flagging an already-active MAG is a no-op success. Reactivates a row that was
-- cleared earlier the same day rather than inserting a duplicate.
--
-- Returns a single row: Status IN ('Set','AlreadySet','LimitReached'), PriorityCount = active count.
CREATE OR ALTER PROCEDURE [dbo].[usp_SetNaukriPriorityMag]
    @AssessmentGroupId INT,
    @MagName           NVARCHAR(200),
    @SetBy             NVARCHAR(150) = NULL,
    @MaxPriority       INT = 2
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);

    IF EXISTS (SELECT 1 FROM NaukriPriorityMag
               WHERE PriorityDate = @Today AND IsActive = 1 AND AssessmentGroupId = @AssessmentGroupId)
    BEGIN
        SELECT CAST('AlreadySet' AS VARCHAR(20)) AS [Status],
               (SELECT COUNT(*) FROM NaukriPriorityMag WHERE PriorityDate = @Today AND IsActive = 1) AS PriorityCount;
        RETURN;
    END

    DECLARE @ActiveCount INT =
        (SELECT COUNT(*) FROM NaukriPriorityMag WHERE PriorityDate = @Today AND IsActive = 1);

    IF @ActiveCount >= @MaxPriority
    BEGIN
        SELECT CAST('LimitReached' AS VARCHAR(20)) AS [Status], @ActiveCount AS PriorityCount;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM NaukriPriorityMag
               WHERE PriorityDate = @Today AND IsActive = 0 AND AssessmentGroupId = @AssessmentGroupId)
    BEGIN
        UPDATE NaukriPriorityMag
        SET IsActive = 1, ClearedAt = NULL, SetBy = @SetBy, SetAt = GETUTCDATE(), MagName = @MagName
        WHERE PriorityDate = @Today AND IsActive = 0 AND AssessmentGroupId = @AssessmentGroupId;
    END
    ELSE
    BEGIN
        INSERT INTO NaukriPriorityMag (AssessmentGroupId, MagName, PriorityDate, SetBy, SetAt, IsActive)
        VALUES (@AssessmentGroupId, @MagName, @Today, @SetBy, GETUTCDATE(), 1);
    END

    SELECT CAST('Set' AS VARCHAR(20)) AS [Status],
           (SELECT COUNT(*) FROM NaukriPriorityMag WHERE PriorityDate = @Today AND IsActive = 1) AS PriorityCount;
END
GO
