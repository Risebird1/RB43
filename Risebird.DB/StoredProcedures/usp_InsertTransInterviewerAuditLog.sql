CREATE OR ALTER PROCEDURE [dbo].[usp_InsertTransInterviewerAuditLog]
    @TransInterviewerId    INT,
    @EventType             VARCHAR(50),
    @FromValue             NVARCHAR(500) = NULL,
    @ToValue               NVARCHAR(500) = NULL,
    @ReasonCategory        NVARCHAR(100) = NULL,
    @TriggeringInterviewId INT           = NULL,
    @ChangedBy             VARCHAR(150)  = 'System'
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TransInterviewerAuditLog
        (TransInterviewerId, EventType, FromValue, ToValue, ReasonCategory, TriggeringInterviewId, ChangedBy, ChangedAt)
    VALUES
        (@TransInterviewerId, @EventType, @FromValue, @ToValue, @ReasonCategory, @TriggeringInterviewId, @ChangedBy, GETUTCDATE());
END
GO
