CREATE OR ALTER PROCEDURE [dbo].[usp_GetIABatchCallMAGSummary]
AS
BEGIN
    SET NOCOUNT ON;

    -- All active MAGs
    ;WITH AllMAGs AS (
        SELECT mag.MAGName AS MagName,
               mag.AssessmentGroupId AS AgId,
               ISNULL(ag.AssessmentGroup, '') AS AssessmentGroupName
        FROM MasterAssessmentGroup mag
        INNER JOIN AssessmentGroup ag ON ag.Id = mag.AssessmentGroupId
        WHERE ISNULL(mag.IsDeleted, 0) = 0
    ),

    -- Counts from InterviewerEmailAvailability
    EACounts AS (
        SELECT
            ea.MagName,
            COUNT(*) AS EATotal,
            SUM(CASE WHEN LEN(ISNULL(COALESCE(ul.MobileNumber, ea.Phone), '')) >= 10 THEN 1 ELSE 0 END) AS EACallable,
            SUM(CASE WHEN ISNULL(ea.PlivoCallStatus, '') <> '' THEN 1 ELSE 0 END) AS EACalled,
            SUM(CASE WHEN ISNULL(ea.PlivoCallStatus, '') = '' AND LEN(ISNULL(COALESCE(ul.MobileNumber, ea.Phone), '')) >= 10 THEN 1 ELSE 0 END) AS EAUncalled,
            SUM(CASE WHEN ea.PlivoIntent = 'available' THEN 1 ELSE 0 END) AS EAAvailable,
            SUM(CASE WHEN ea.PlivoIntent = 'not_available' THEN 1 ELSE 0 END) AS EANotAvailable
        FROM InterviewerEmailAvailability ea
        LEFT JOIN UserList ul ON ea.UserId = ul.Id
        WHERE ea.MagName IS NOT NULL AND ea.MagName <> ''
          AND ea.Status IN ('Open', 'InProgress', 'Verified')
        GROUP BY ea.MagName
    ),

    -- Distinct EA InterviewerProfileIds per MAG (for dedup)
    EAProfileIds AS (
        SELECT ea.MagName, ea.InterviewerProfileId
        FROM InterviewerEmailAvailability ea
        WHERE ea.InterviewerProfileId IS NOT NULL
          AND ea.MagName IS NOT NULL AND ea.MagName <> ''
          AND ea.Status IN ('Open', 'InProgress', 'Verified')
        GROUP BY ea.MagName, ea.InterviewerProfileId
    ),

    -- Counts from InterviewerProfile via AssessmentGroupInterviewers, excluding those already in EA
    ProfileCounts AS (
        SELECT
            m.MagName,
            COUNT(*) AS ProfTotal,
            SUM(CASE WHEN LEN(ISNULL(ul.MobileNumber, '')) >= 10 THEN 1 ELSE 0 END) AS ProfCallable
        FROM AllMAGs m
        INNER JOIN AssessmentGroupInterviewers agi ON agi.AssessmentGroupId = m.AgId
        INNER JOIN InterviewerProfile ip ON ip.Id = agi.InterviewerProfileId
        INNER JOIN UserList ul ON ul.Id = ip.UserId
        WHERE ISNULL(ip.IsDeleted, 0) = 0
          AND ISNULL(ip.Isavailable, 0) <> 4
          AND NOT EXISTS (
              SELECT 1 FROM EAProfileIds ep
              WHERE ep.MagName = m.MagName AND ep.InterviewerProfileId = ip.Id
          )
        GROUP BY m.MagName
    ),

    -- Batch log info
    BatchInfo AS (
        SELECT bl.MagName, MAX(bl.Id) AS LastBatchId, COUNT(*) AS TotalBatches
        FROM IABatchCallLog bl
        GROUP BY bl.MagName
    )

    SELECT
        m.MagName,
        m.AssessmentGroupName,
        ISNULL(ec.EATotal, 0) + ISNULL(pc.ProfTotal, 0) AS TotalInterviewers,
        ISNULL(ec.EACallable, 0) + ISNULL(pc.ProfCallable, 0) AS CallableCount,
        ISNULL(ec.EACalled, 0) AS AlreadyCalled,
        ISNULL(ec.EAUncalled, 0) + ISNULL(pc.ProfCallable, 0) AS UncalledCount,
        ISNULL(ec.EAAvailable, 0) AS Available,
        ISNULL(ec.EANotAvailable, 0) AS NotAvailable,
        lb.InitiatedBy AS LastBatchBy,
        lb.InitiatedAt AS LastBatchAt,
        lb.InterviewersAttempted AS LastBatchCount,
        lb.InterviewersSucceeded AS LastBatchSucceeded,
        lb.Status AS LastBatchStatus,
        ISNULL(bi.TotalBatches, 0) AS TotalBatches
    FROM AllMAGs m
    LEFT JOIN EACounts ec ON ec.MagName = m.MagName
    LEFT JOIN ProfileCounts pc ON pc.MagName = m.MagName
    LEFT JOIN BatchInfo bi ON bi.MagName = m.MagName
    LEFT JOIN IABatchCallLog lb ON lb.Id = bi.LastBatchId
    WHERE ISNULL(ec.EATotal, 0) + ISNULL(pc.ProfTotal, 0) > 0
    ORDER BY
        CASE WHEN ISNULL(ec.EATotal, 0) > 0 THEN 0 ELSE 1 END,
        ISNULL(ec.EATotal, 0) + ISNULL(pc.ProfTotal, 0) DESC
END
GO
