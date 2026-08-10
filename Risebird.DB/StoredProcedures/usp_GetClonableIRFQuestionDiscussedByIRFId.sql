IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableIRFQuestionDiscussedByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableIRFQuestionDiscussedByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClonableIRFQuestionDiscussedByIRFId]
    @irfId INT
AS
BEGIN
    SELECT sk.Name AS PrimarySkillInvite, ts.Question AS Comment
    FROM ClonableTemplateQuestions ts
    JOIN Skill sk ON sk.id = ts.Skill
    WHERE ts.ClonableIRFId = @irfId
END
GO
