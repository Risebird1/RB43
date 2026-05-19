IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetBatchCallMAGSummary')
    DROP PROCEDURE usp_GetBatchCallMAGSummary
GO

CREATE PROCEDURE [dbo].[usp_GetBatchCallMAGSummary]
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH MagStats AS (
        SELECT
            np.MagName,
            COUNT(*)                       AS TotalProspects,
            SUM(CASE WHEN np.ParsedPhone IS NOT NULL AND LEN(LTRIM(RTRIM(np.ParsedPhone))) >= 10
                 THEN 1 ELSE 0 END)        AS CallableCount,
            SUM(CASE WHEN np.PlivoCallStatus IS NOT NULL AND np.PlivoCallStatus <> ''
                 THEN 1 ELSE 0 END)        AS AlreadyCalled,
            SUM(CASE WHEN np.ParsedPhone IS NOT NULL AND LEN(LTRIM(RTRIM(np.ParsedPhone))) >= 10
                      AND ISNULL(np.PlivoCallStatus, '') = ''
                 THEN 1 ELSE 0 END)        AS UncalledCount,
            SUM(CASE WHEN LOWER(ISNULL(np.PlivoProspectDisposition, '')) = 'interested'
                 THEN 1 ELSE 0 END)        AS Interested,
            SUM(CASE WHEN LOWER(ISNULL(np.PlivoProspectDisposition, '')) IN ('not_interested', 'unsubscribe')
                 THEN 1 ELSE 0 END)        AS NotInterested
        FROM NaukriProspectiveInterviewer np
        WHERE np.MagName IS NOT NULL AND np.MagName <> ''
        GROUP BY np.MagName
    ),
    MagAG AS (
        SELECT mag.MAGName, ISNULL(ag.AssessmentGroup, '') AS AssessmentGroupName
        FROM MasterAssessmentGroup mag
        INNER JOIN AssessmentGroup ag ON ag.Id = mag.AssessmentGroupId
        WHERE ISNULL(mag.IsDeleted, 0) = 0
    ),
    LatestBatch AS (
        SELECT MagName, InitiatedBy, InitiatedAt, ProspectsAttempted, ProspectsSucceeded, Status,
               ROW_NUMBER() OVER (PARTITION BY MagName ORDER BY InitiatedAt DESC) AS rn
        FROM NaukriBatchCallLog
    ),
    BatchAgg AS (
        SELECT MagName, COUNT(*) AS TotalBatches
        FROM NaukriBatchCallLog
        GROUP BY MagName
    )
    SELECT
        ms.MagName,
        ISNULL(ma.AssessmentGroupName, '') AS AssessmentGroupName,
        ms.TotalProspects,
        ms.CallableCount,
        ms.AlreadyCalled,
        ms.UncalledCount,
        ms.Interested,
        ms.NotInterested,
        lb.InitiatedBy       AS LastBatchBy,
        lb.InitiatedAt       AS LastBatchAt,
        lb.ProspectsAttempted AS LastBatchCount,
        lb.ProspectsSucceeded AS LastBatchSucceeded,
        lb.Status            AS LastBatchStatus,
        ISNULL(ba.TotalBatches, 0) AS TotalBatches
    FROM MagStats ms
    LEFT JOIN MagAG ma         ON ma.MAGName = ms.MagName
    LEFT JOIN LatestBatch lb   ON lb.MagName = ms.MagName AND lb.rn = 1
    LEFT JOIN BatchAgg ba      ON ba.MagName = ms.MagName
    ORDER BY ms.CallableCount DESC, ms.MagName ASC
END
GO
