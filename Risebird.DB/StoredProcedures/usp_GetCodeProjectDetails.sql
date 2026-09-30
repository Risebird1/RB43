IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeProjectDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeProjectDetails
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetCodeProjectDetails
    @QuestionId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cpd.*, q.IRFId, q.SkillName, q.Comment, q.QuestionGist, q.CodingTimeLimitInMinutes, q.GeneratedPayload
    FROM CodeProjectDetails cpd
    INNER JOIN SkillChannelSampleQuestions q ON q.id = cpd.QuestionId
    WHERE cpd.QuestionId = @QuestionId;
END
GO
