IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAIGeneratedSampleQuestion'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAIGeneratedSampleQuestion
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddAIGeneratedSampleQuestion]
    @IRFId int,
    @SkillName nvarchar(500),
    @Comment nvarchar(max),
    @ExpectedAnswer nvarchar(max),
    @GeneratedPayload nvarchar(max),
    @CreatedBy varchar(150),
    @QuestionGist nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SkillChannelSampleQuestions
        (IRFId, Skill, SkillName, Comment, ExpectedAnswer, GeneratedPayload, IsGeneratedByAi, CreatedBy, QuestionGist)
    VALUES
        (@IRFId, 0, @SkillName, @Comment, @ExpectedAnswer, @GeneratedPayload, 1, @CreatedBy, @QuestionGist)

    SELECT SCOPE_IDENTITY() AS NewId
END
GO
