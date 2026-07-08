IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriMagAssignmentSummary')
    DROP PROCEDURE usp_GetNaukriMagAssignmentSummary
GO

-- Per-MAG breakdown used by the "Bulk assign by MAG" feature. Prospects are grouped by their
-- normalized MAG name (tolerant of casing / stray whitespace via dbo.fn_NormalizeMagName) so the
-- operator can pick a MAG and see how many tickets it holds and how they are currently distributed.
--
--   Total      – all prospects in the MAG
--   Assigned   – prospects that already have an owner
--   Unassigned – prospects with no owner
--   Active     – open / in-progress tickets (not Closed, not Onboarded) — the ones that still need work
--
-- PERFORMANCE: dbo.fn_NormalizeMagName is a scalar UDF (RBAR), so it must NOT be applied per row over
-- the whole prospect table (~tens of thousands of rows). Instead we first aggregate by the RAW MagName
-- (a cheap GROUP BY), then normalize only the small set of distinct MagName values, and resolve the
-- assessment-group name from a one-time normalized lookup table.
CREATE PROCEDURE [dbo].[usp_GetNaukriMagAssignmentSummary]
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) Cheap aggregate over the prospect table on the RAW MagName column (no UDF).
    IF OBJECT_ID('tempdb..#RawMag') IS NOT NULL DROP TABLE #RawMag;
    SELECT
        np.MagName,
        COUNT(*)                                                        AS Total,
        SUM(CASE WHEN np.AssignedToUserId IS NOT NULL THEN 1 ELSE 0 END) AS Assigned,
        SUM(CASE WHEN np.AssignedToUserId IS NULL     THEN 1 ELSE 0 END) AS Unassigned,
        SUM(CASE WHEN ISNULL(np.FunnelStage, '') <> 'Onboarded'
                  AND ISNULL(np.TicketStatus, 'Open') <> 'Closed' THEN 1 ELSE 0 END) AS Active
    INTO #RawMag
    FROM dbo.NaukriProspectiveInterviewer np
    WHERE np.MagName IS NOT NULL AND LTRIM(RTRIM(np.MagName)) <> ''
    GROUP BY np.MagName;

    -- 2) Normalize only the (small) distinct set of MagName values.
    IF OBJECT_ID('tempdb..#NormMag') IS NOT NULL DROP TABLE #NormMag;
    SELECT
        dbo.fn_NormalizeMagName(r.MagName) AS NormMag,
        r.MagName, r.Total, r.Assigned, r.Unassigned, r.Active
    INTO #NormMag
    FROM #RawMag r;

    -- 3) One-time normalized assessment-group lookup (Pref 1 = via MAG, Pref 2 = direct AG name).
    IF OBJECT_ID('tempdb..#AgLookup') IS NOT NULL DROP TABLE #AgLookup;
    SELECT NormName, Pref, AssessmentGroupName
    INTO #AgLookup
    FROM (
        SELECT dbo.fn_NormalizeMagName(mag.MAGName) AS NormName, 1 AS Pref, ag.AssessmentGroup AS AssessmentGroupName
        FROM dbo.MasterAssessmentGroup mag
        LEFT JOIN dbo.AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
        WHERE (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
        UNION ALL
        SELECT dbo.fn_NormalizeMagName(ag2.AssessmentGroup) AS NormName, 2 AS Pref, ag2.AssessmentGroup AS AssessmentGroupName
        FROM dbo.AssessmentGroup ag2
    ) x
    WHERE x.AssessmentGroupName IS NOT NULL;

    -- 4) Collapse whitespace/case variants into one row per normalized MAG + attach the AG name.
    SELECT
        MIN(n.MagName)      AS MagName,
        ag.AssessmentGroupName,
        SUM(n.Total)        AS Total,
        SUM(n.Assigned)     AS Assigned,
        SUM(n.Unassigned)   AS Unassigned,
        SUM(n.Active)       AS Active
    FROM #NormMag n
    OUTER APPLY (
        SELECT TOP 1 a.AssessmentGroupName
        FROM #AgLookup a
        WHERE a.NormName = n.NormMag
        ORDER BY a.Pref
    ) ag
    GROUP BY n.NormMag, ag.AssessmentGroupName
    ORDER BY Active DESC, Total DESC, MagName;

    IF OBJECT_ID('tempdb..#RawMag')  IS NOT NULL DROP TABLE #RawMag;
    IF OBJECT_ID('tempdb..#NormMag') IS NOT NULL DROP TABLE #NormMag;
    IF OBJECT_ID('tempdb..#AgLookup') IS NOT NULL DROP TABLE #AgLookup;
END
GO
