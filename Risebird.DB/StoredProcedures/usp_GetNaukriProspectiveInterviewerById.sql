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
        mag.AssessmentGroupId AS ResolvedAssessmentGroupId,
        ag.AssessmentGroup    AS AssessmentGroupName,
        1                     AS TotalCount
    FROM NaukriProspectiveInterviewer np
    LEFT JOIN MasterAssessmentGroup mag
        ON LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(np.MagName)))
        AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
    LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
    WHERE np.Id = @Id
END
GO
