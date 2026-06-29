IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriProspectiveInterviewerById')
    DROP PROCEDURE usp_GetNaukriProspectiveInterviewerById
GO

CREATE PROCEDURE [dbo].[usp_GetNaukriProspectiveInterviewerById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        np.*,
        magInfo.AssessmentGroupId   AS ResolvedAssessmentGroupId,
        magInfo.AssessmentGroupName AS AssessmentGroupName,
        1                           AS TotalCount
    FROM NaukriProspectiveInterviewer np
    OUTER APPLY (
        -- Resolve the prospect's MAG name to an assessment group, tolerant of casing and the
        -- invisible/odd whitespace that relay text carries (see dbo.fn_NormalizeMagName).
        -- Preferred match is on MasterAssessmentGroup.MAGName; if that fails, fall back to a
        -- direct AssessmentGroup name match (covers names that exist as an AG but have no MAG row).
        SELECT TOP 1 x.AssessmentGroupId, x.AssessmentGroupName
        FROM (
            SELECT 1 AS Pref, mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
            FROM MasterAssessmentGroup mag
            LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
            WHERE dbo.fn_NormalizeMagName(mag.MAGName) = dbo.fn_NormalizeMagName(np.MagName)
              AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
            UNION ALL
            SELECT 2 AS Pref, ag2.id AS AssessmentGroupId, ag2.AssessmentGroup AS AssessmentGroupName
            FROM AssessmentGroup ag2
            WHERE dbo.fn_NormalizeMagName(ag2.AssessmentGroup) = dbo.fn_NormalizeMagName(np.MagName)
        ) x
        WHERE x.AssessmentGroupId IS NOT NULL
        ORDER BY x.Pref
    ) magInfo
    WHERE np.Id = @Id
END
GO
