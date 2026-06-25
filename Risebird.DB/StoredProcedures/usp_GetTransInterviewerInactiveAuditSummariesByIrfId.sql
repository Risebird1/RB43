CREATE OR ALTER PROCEDURE [dbo].[usp_GetTransInterviewerInactiveAuditSummariesByIrfId]
    @IrfId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NULL
        RETURN;

    SELECT
        ti.id AS TransInterviewerId,
        CAST(1 AS BIT) AS InactiveAuditSummaryLoaded,
        latest.InactiveAuditEventType,
        latest.InactiveAuditReasonDetail,
        latest.InactiveAuditTriggeringInterviewId,
        latest.InactiveAuditChangedAt
    FROM dbo.TransInterviewer ti
    OUTER APPLY (
        SELECT TOP 1
            al.EventType AS InactiveAuditEventType,
            CASE
                WHEN al.EventType = 'AutoQcRemoval' THEN al.ToValue
                ELSE al.ReasonCategory
            END AS InactiveAuditReasonDetail,
            al.TriggeringInterviewId AS InactiveAuditTriggeringInterviewId,
            al.ChangedAt AS InactiveAuditChangedAt
        FROM dbo.TransInterviewerAuditLog al
        WHERE al.TransInterviewerId = ti.id
          AND al.EventType IN ('RbVetFlippedToNo', 'ClientVetFlippedToNo', 'AutoQcRemoval')
        ORDER BY al.ChangedAt DESC, al.Id DESC
    ) latest
    WHERE ti.IRFId = @IrfId;
END
GO
