IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetQuestionBankForJD'))
BEGIN
    DROP PROCEDURE dbo.usp_GetQuestionBankForJD
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetQuestionBankForJD]
    @IRFId int
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ChannelSkills AS (
        SELECT DISTINCT LTRIM(RTRIM(tts.SkillName)) AS SkillName
        FROM dbo.TransTechSkill tts
        WHERE tts.IRFId = @IRFId
          AND tts.SkillName IS NOT NULL
          AND LTRIM(RTRIM(tts.SkillName)) <> ''
    )
    SELECT DISTINCT
        LTRIM(RTRIM(bank.Comment)) AS Question,
        LTRIM(RTRIM(bank.SkillName)) AS SkillName
    FROM (
        SELECT tsc.Comment, tsc.SkillName
        FROM dbo.TechSkillComments tsc
        INNER JOIN ChannelSkills cs ON LTRIM(RTRIM(tsc.SkillName)) = cs.SkillName
        WHERE tsc.Comment IS NOT NULL
          AND LTRIM(RTRIM(tsc.Comment)) <> ''

        UNION

        SELECT scsq.Comment, scsq.SkillName
        FROM dbo.SkillChannelSampleQuestions scsq
        INNER JOIN ChannelSkills cs ON LTRIM(RTRIM(scsq.SkillName)) = cs.SkillName
        WHERE scsq.Comment IS NOT NULL
          AND LTRIM(RTRIM(scsq.Comment)) <> ''
          AND ISNULL(scsq.IsAIGenerated, 0) = 0
    ) bank
    WHERE bank.Comment IS NOT NULL
      AND LTRIM(RTRIM(bank.Comment)) <> '';
END
GO
