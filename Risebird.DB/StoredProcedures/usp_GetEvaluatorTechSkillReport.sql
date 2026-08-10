IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEvaluatorTechSkillReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEvaluatorTechSkillReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEvaluatorTechSkillReport]
    @CandidateId INT,
    @IRFId       INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.*,
        sk.[Name] AS SkillName
    FROM (
        SELECT ts.*, lt.[Name] AS RequiredLevel
        FROM dbo.TechSkillEvaluatorReport ts
        INNER JOIN dbo.LevelTech lt ON ts.RequiredLevelId = lt.id
        WHERE ts.CandidateId = @CandidateId
          AND ts.IRFId = @IRFId
    ) a
    INNER JOIN dbo.Skill sk ON a.SkillId = sk.id;
END
GO
