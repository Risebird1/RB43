IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetAllMasterAssessmentGroups')
    DROP PROCEDURE usp_GetAllMasterAssessmentGroups
GO

CREATE PROCEDURE [dbo].[usp_GetAllMasterAssessmentGroups]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        mag.id              AS MagId,
        mag.MAGName         AS MagName,
        mag.AssessmentGroupId,
        ag.AssessmentGroup  AS AssessmentGroupName
    FROM MasterAssessmentGroup mag
    INNER JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
    WHERE (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
    ORDER BY mag.MAGName
END
GO
