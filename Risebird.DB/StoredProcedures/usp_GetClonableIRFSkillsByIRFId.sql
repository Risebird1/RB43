IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableIRFSkillsByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableIRFSkillsByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClonableIRFSkillsByIRFId]
    @IRFId INT
AS
BEGIN
    SELECT tts.*, Skill.[Name] AS SkillName
    FROM TransClonableIRFTechSkill tts
    JOIN Skill ON tts.SkillId = Skill.id
    WHERE ClonableIRFId = @IRFId
END
GO
