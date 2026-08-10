IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransTechSkillByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransTechSkillByIRFId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransTechSkillByIRFId]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tts.*, irf.TechRubricType
    FROM dbo.TransTechSkill tts
    INNER JOIN dbo.IRF irf ON irf.id = tts.IRFId
    WHERE tts.IRFId = @IRFId;
END
GO
