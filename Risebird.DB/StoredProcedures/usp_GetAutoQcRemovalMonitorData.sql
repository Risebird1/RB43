CREATE OR ALTER PROCEDURE [dbo].[usp_GetAutoQcRemovalMonitorData]
    @FromUtc DATETIME = NULL,
    @ToUtc DATETIME = NULL,
    @IrfId INT = NULL,
    @AuditSource VARCHAR(50) = NULL,
    @Verdict VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NULL
        RETURN;

    IF @FromUtc IS NULL
        SET @FromUtc = DATEADD(DAY, -30, GETUTCDATE());
    IF @ToUtc IS NULL
        SET @ToUtc = GETUTCDATE();

    ;WITH Removals AS (
        SELECT
            al.Id AS AuditLogId,
            al.ChangedAt AS RemovedAt,
            ip.NickName AS InterviewerCodedId,
            ti.IRFId AS IrfId,
            irf.IRFId AS IrfName,
            al.TriggeringInterviewId,
            al.ToValue AS Verdict,
            al.AuditSource AS Source,
            al.BackfillBatchId,
            al.ExtraMetadata,
            al.TransInterviewerId
        FROM dbo.TransInterviewerAuditLog al
        INNER JOIN dbo.TransInterviewer ti ON ti.id = al.TransInterviewerId
        LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ti.UserId
        LEFT JOIN dbo.IRF irf ON irf.id = ti.IRFId
        WHERE al.EventType = 'AutoQcRemoval'
          AND al.ChangedAt >= @FromUtc
          AND al.ChangedAt <= @ToUtc
          AND (@IrfId IS NULL OR ti.IRFId = @IrfId)
          AND (@AuditSource IS NULL OR al.AuditSource = @AuditSource)
          AND (@Verdict IS NULL OR al.ToValue = @Verdict)
    ),
    Reactivations AS (
        SELECT al.TransInterviewerId, MAX(al.ChangedAt) AS ReactivatedAt
        FROM dbo.TransInterviewerAuditLog al
        WHERE al.EventType = 'RbVetFlippedToYes'
        GROUP BY al.TransInterviewerId
    )
    SELECT
        r.AuditLogId,
        r.RemovedAt,
        r.InterviewerCodedId,
        r.IrfId,
        r.IrfName,
        r.TriggeringInterviewId,
        r.Verdict,
        r.Source,
        r.BackfillBatchId,
        r.ExtraMetadata,
        CAST(CASE WHEN react.ReactivatedAt IS NOT NULL AND react.ReactivatedAt > r.RemovedAt THEN 1 ELSE 0 END AS BIT) AS WasReactivated
    FROM Removals r
    LEFT JOIN Reactivations react ON react.TransInterviewerId = r.TransInterviewerId
    ORDER BY r.RemovedAt DESC, r.AuditLogId DESC;

    SELECT
        ti.IRFId AS IrfId,
        irf.IRFId AS IrfName,
        COUNT(1) AS Removals,
        SUM(CASE WHEN react.ReactivatedAt IS NOT NULL AND react.ReactivatedAt > al.ChangedAt THEN 1 ELSE 0 END) AS Reactivations,
        SUM(CASE WHEN al.ToValue = 'suspicious' THEN 1 ELSE 0 END) AS SuspiciousRemovals,
        SUM(CASE WHEN al.ToValue = 'redo' THEN 1 ELSE 0 END) AS RedoRemovals
    FROM dbo.TransInterviewerAuditLog al
    INNER JOIN dbo.TransInterviewer ti ON ti.id = al.TransInterviewerId
    LEFT JOIN dbo.IRF irf ON irf.id = ti.IRFId
    OUTER APPLY (
        SELECT MAX(x.ChangedAt) AS ReactivatedAt
        FROM dbo.TransInterviewerAuditLog x
        WHERE x.TransInterviewerId = al.TransInterviewerId
          AND x.EventType = 'RbVetFlippedToYes'
    ) react
    WHERE al.EventType = 'AutoQcRemoval'
      AND al.ChangedAt >= @FromUtc
      AND al.ChangedAt <= @ToUtc
      AND (@IrfId IS NULL OR ti.IRFId = @IrfId)
      AND (@AuditSource IS NULL OR al.AuditSource = @AuditSource)
      AND (@Verdict IS NULL OR al.ToValue = @Verdict)
    GROUP BY ti.IRFId, irf.IRFId
    ORDER BY Removals DESC, ti.IRFId;
END
GO
