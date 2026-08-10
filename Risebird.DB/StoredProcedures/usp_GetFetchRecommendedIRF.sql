IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetFetchRecommendedIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetFetchRecommendedIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetFetchRecommendedIRF]
AS
BEGIN
    SELECT
        ir.id,
        IRF.IRFId,
        ag.AssessmentGroup,
        ir.AssessmentGroupId,
        ir.CompetencyLevelId,
        ir.CountryType,
        ir.SeniorityLevelId
    FROM IRFRecommendations ir
    JOIN ClonableSkillChannel cs ON cs.id = ir.CloneIRFId
    JOIN IRF ON cs.CreatedFromIRFId = IRF.id
    LEFT JOIN AssessmentGroup ag ON ag.id = ir.AssessmentGroupId
    ORDER BY ir.id DESC
END
GO
