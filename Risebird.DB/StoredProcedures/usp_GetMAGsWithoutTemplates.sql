-- usp_GetMAGsWithoutTemplates
-- Returns all active MasterAssessmentGroups that don't have a template.
-- Used to identify MAGs that need templates to be created.
-- Checks both: templates linked through IRF AND templates with direct AssessmentGroupId.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetMAGsWithoutTemplates]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        mag.id AS MasterAssessmentGroupId,
        mag.MAGName,
        mag.AssessmentGroupId,
        ag.AssessmentGroup AS AssessmentGroupName,
        ag.PrimarySkills,
        mag.CreatedOn,
        mag.CreatedBy
    FROM dbo.MasterAssessmentGroup mag
    INNER JOIN dbo.AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
    WHERE (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
      -- Check 1: No template linked through IRF
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ClonableSkillChannel csc
          INNER JOIN dbo.IRF irf ON irf.id = csc.CreatedFromIRFId
          WHERE irf.AssessmentGroupId = mag.AssessmentGroupId
            AND (csc.IsDeleted IS NULL OR csc.IsDeleted = 0)
      )
      -- Check 2: No template with direct AssessmentGroupId (created by usp_CreateTemplateFromMAG)
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ClonableSkillChannel csc
          WHERE csc.AssessmentGroupId = mag.AssessmentGroupId
            AND (csc.IsDeleted IS NULL OR csc.IsDeleted = 0)
      )
    ORDER BY mag.MAGName;
END
GO
