CREATE OR ALTER PROCEDURE [dbo].[usp_HasAutoQcFlagAuditForInterview]
    @TransInterviewerId    INT,
    @TriggeringInterviewId INT,
    @AuditSource           VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS AlreadyRecorded;
        RETURN;
    END

    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.TransInterviewerAuditLog al
        WHERE al.TransInterviewerId = @TransInterviewerId
          AND al.EventType = 'AutoQcFlag'
          AND al.TriggeringInterviewId = @TriggeringInterviewId
          AND (@AuditSource IS NULL OR al.AuditSource = @AuditSource)
    ) THEN 1 ELSE 0 END AS BIT) AS AlreadyRecorded;
END
GO
