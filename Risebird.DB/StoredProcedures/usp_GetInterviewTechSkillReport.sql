IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewTechSkillReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewTechSkillReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewTechSkillReport]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ts.*,
        req.[Name] AS RequiredLevel,
        act.[Name] AS ActualLevel
    FROM dbo.TechSkillReport ts
    INNER JOIN dbo.LevelTech req ON ts.RequiredLevelId = req.id
    LEFT JOIN dbo.LevelTech act ON ts.ActualLevelId = act.id
    WHERE ts.InterviewId = @InterviewId;
END
GO
