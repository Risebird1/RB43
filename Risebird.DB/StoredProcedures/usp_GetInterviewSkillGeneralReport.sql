IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewSkillGeneralReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewSkillGeneralReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewSkillGeneralReport]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sgr.*,
        sgr.Type AS Name,
        lt.[Name] AS RequiredLevel
    FROM dbo.SkillGeneralReport sgr
    INNER JOIN dbo.LevelTech lt ON sgr.RequiredLevelId = lt.id
    WHERE sgr.InterviewId = @InterviewId;
END
GO
