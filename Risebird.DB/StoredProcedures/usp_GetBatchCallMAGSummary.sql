IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetBatchCallMAGSummary')
    DROP PROCEDURE usp_GetBatchCallMAGSummary
GO

CREATE PROCEDURE [dbo].[usp_GetBatchCallMAGSummary]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        np.MagName,
        ISNULL(ag.AssessmentGroup, '') AS AssessmentGroupName,
        COUNT(*)                       AS TotalProspects,
        SUM(CASE WHEN np.ParsedPhone IS NOT NULL AND LEN(LTRIM(RTRIM(np.ParsedPhone))) >= 10
             THEN 1 ELSE 0 END)        AS CallableCount,
        SUM(CASE WHEN np.PlivoCallStatus IS NOT NULL
             THEN 1 ELSE 0 END)        AS AlreadyCalled,
        SUM(CASE WHEN LOWER(ISNULL(np.PlivoProspectDisposition, '')) = 'interested'
             THEN 1 ELSE 0 END)        AS Interested,
        SUM(CASE WHEN LOWER(ISNULL(np.PlivoProspectDisposition, '')) IN ('not_interested', 'unsubscribe')
             THEN 1 ELSE 0 END)        AS NotInterested,
        lb.InitiatedBy                 AS LastBatchBy,
        lb.InitiatedAt                 AS LastBatchAt,
        lb.ProspectsAttempted           AS LastBatchCount,
        lb.ProspectsSucceeded           AS LastBatchSucceeded,
        lb.Status                       AS LastBatchStatus,
        ISNULL(ba.TotalBatches, 0)     AS TotalBatches
    FROM NaukriProspectiveInterviewer np
    LEFT JOIN MasterAssessmentGroup mag
        ON mag.MAGName = np.MagName AND ISNULL(mag.IsDeleted, 0) = 0
    LEFT JOIN AssessmentGroup ag
        ON ag.Id = mag.AssessmentGroupId
    OUTER APPLY (
        SELECT TOP 1 InitiatedBy, InitiatedAt, ProspectsAttempted, ProspectsSucceeded, Status
        FROM NaukriBatchCallLog
        WHERE MagName = np.MagName
        ORDER BY InitiatedAt DESC
    ) lb
    OUTER APPLY (
        SELECT COUNT(*) AS TotalBatches
        FROM NaukriBatchCallLog
        WHERE MagName = np.MagName
    ) ba
    WHERE np.MagName IS NOT NULL AND np.MagName <> ''
    GROUP BY np.MagName, ag.AssessmentGroup,
             lb.InitiatedBy, lb.InitiatedAt, lb.ProspectsAttempted, lb.ProspectsSucceeded, lb.Status,
             ba.TotalBatches
    ORDER BY
        SUM(CASE WHEN np.ParsedPhone IS NOT NULL AND LEN(LTRIM(RTRIM(np.ParsedPhone))) >= 10
             THEN 1 ELSE 0 END) DESC,
        np.MagName ASC
END
GO
