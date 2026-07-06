IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEvaluatorSkillGeneralReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEvaluatorSkillGeneralReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEvaluatorSkillGeneralReport]
    @CandidateId INT,
    @IRFId       INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sgr.*,
        sgr.[Type] AS [Name],
        lt.[Name] AS RequiredLevel
    FROM dbo.SkillGeneralEvaluatorReport sgr
    INNER JOIN dbo.LevelTech lt ON sgr.RequiredLevelId = lt.id
    WHERE sgr.CandidateId = @CandidateId
      AND sgr.IRFId = @IRFId;
END
GO
