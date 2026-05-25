CREATE OR ALTER PROCEDURE [dbo].[usp_GetIABatchCallMAGSummary]
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH InterviewerCounts AS (
        SELECT
            ea.MagName,
            COUNT(*) AS TotalInterviewers,
            SUM(CASE WHEN LEN(ISNULL(COALESCE(ul.MobileNumber, ea.Phone), '')) >= 10 THEN 1 ELSE 0 END) AS CallableCount,
            SUM(CASE WHEN ISNULL(ea.PlivoCallStatus, '') <> '' THEN 1 ELSE 0 END) AS AlreadyCalled,
            SUM(CASE WHEN ISNULL(ea.PlivoCallStatus, '') = '' AND LEN(ISNULL(COALESCE(ul.MobileNumber, ea.Phone), '')) >= 10 THEN 1 ELSE 0 END) AS UncalledCount,
            SUM(CASE WHEN ea.PlivoIntent = 'available' THEN 1 ELSE 0 END) AS Available,
            SUM(CASE WHEN ea.PlivoIntent = 'not_available' THEN 1 ELSE 0 END) AS NotAvailable
        FROM InterviewerEmailAvailability ea
        LEFT JOIN UserList ul ON ea.UserId = ul.Id
        WHERE ea.MagName IS NOT NULL AND ea.MagName <> ''
          AND ea.Status IN ('Open', 'InProgress', 'Verified')
        GROUP BY ea.MagName
    ),
    BatchInfo AS (
        SELECT
            bl.MagName,
            MAX(bl.Id) AS LastBatchId,
            COUNT(*) AS TotalBatches
        FROM IABatchCallLog bl
        GROUP BY bl.MagName
    )
    ,MagAG AS (
        SELECT mag.MAGName, ISNULL(ag.AssessmentGroup, '') AS AssessmentGroupName
        FROM MasterAssessmentGroup mag
        INNER JOIN AssessmentGroup ag ON ag.Id = mag.AssessmentGroupId
        WHERE ISNULL(mag.IsDeleted, 0) = 0
    )
    SELECT
        ic.MagName,
        ISNULL(ma.AssessmentGroupName, '') AS AssessmentGroupName,
        ic.TotalInterviewers,
        ic.CallableCount,
        ic.AlreadyCalled,
        ic.UncalledCount,
        ic.Available,
        ic.NotAvailable,
        lb.InitiatedBy AS LastBatchBy,
        lb.InitiatedAt AS LastBatchAt,
        lb.InterviewersAttempted AS LastBatchCount,
        lb.InterviewersSucceeded AS LastBatchSucceeded,
        lb.Status AS LastBatchStatus,
        ISNULL(bi.TotalBatches, 0) AS TotalBatches
    FROM InterviewerCounts ic
    LEFT JOIN MagAG ma ON ma.MAGName = ic.MagName
    LEFT JOIN BatchInfo bi ON bi.MagName = ic.MagName
    LEFT JOIN IABatchCallLog lb ON lb.Id = bi.LastBatchId
    ORDER BY ic.TotalInterviewers DESC
END
GO
