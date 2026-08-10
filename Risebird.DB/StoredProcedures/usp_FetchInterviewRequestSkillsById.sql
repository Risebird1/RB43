IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchInterviewRequestSkillsById'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewRequestSkillsById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchInterviewRequestSkillsById]
    @id INT
AS
BEGIN
    SELECT IRFId, LevelId, SkillId, ActualLevelId AS Weightage
    FROM TransInterviewRequestSkills
    WHERE IRFID = @id
END
GO
