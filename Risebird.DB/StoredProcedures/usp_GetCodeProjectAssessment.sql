IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeProjectAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeProjectAssessment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetCodeProjectAssessment
    @SessionId VARCHAR(50),
    @QuestionId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, SessionId, QuestionId, QuestionText, CodeLanguage, SubmittedCode, TimeLimitInMinutes,
           TimeStarted, TimeCompleted, TimeTakenInSeconds, IsTimeExpired, VMPropertyId,
           RequirementsPassed, RequirementsTotal, ResultsJson
    FROM CodingAssessment
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;
END
GO
