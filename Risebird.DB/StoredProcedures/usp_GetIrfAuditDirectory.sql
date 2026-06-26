CREATE OR ALTER PROCEDURE [dbo].[usp_GetIrfAuditDirectory]
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Completed AS (
        SELECT
            irf.id AS IrfId,
            irf.IRFId AS IrfName,
            COUNT(DISTINCT im.id) AS TotalCompletedLifetime,
            COUNT(DISTINCT CASE WHEN im.interviewdate >= DATEADD(DAY, -90, GETUTCDATE()) THEN im.id END) AS CompletedLast90Days
        FROM dbo.IRF irf
        INNER JOIN dbo.InterviewMeetings im ON im.IRFId = irf.IRFId
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE ic.MeetingStatus IN (4, 6, 7, 8)
        GROUP BY irf.id, irf.IRFId
        HAVING COUNT(DISTINCT im.id) >= 1
    ),
    PendingFlags AS (
        SELECT t.IRFId AS IrfId, COUNT(*) AS PendingFlagsCount
        FROM dbo.TransInterviewer t
        WHERE t.IsActive = 1
          AND NULLIF(LTRIM(RTRIM(ISNULL(t.AutoQcVerdict, ''))), '') IS NOT NULL
        GROUP BY t.IRFId
    ),
    LastBackfill AS (
        SELECT IrfId, MAX(ExecutedAtUtc) AS LastBackfillAtUtc
        FROM dbo.AutoQcIrfBackfillRun
        GROUP BY IrfId
    )
    SELECT
        c.IrfId,
        c.IrfName,
        ISNULL(c.TotalCompletedLifetime, 0) AS TotalCompletedLifetime,
        ISNULL(c.CompletedLast90Days, 0) AS CompletedLast90Days,
        ISNULL(p.PendingFlagsCount, 0) AS PendingFlagsCount,
        lb.LastBackfillAtUtc
    FROM Completed c
    LEFT JOIN PendingFlags p ON p.IrfId = c.IrfId
    LEFT JOIN LastBackfill lb ON lb.IrfId = c.IrfId
    ORDER BY ISNULL(p.PendingFlagsCount, 0) DESC, c.IrfName;
END
GO
