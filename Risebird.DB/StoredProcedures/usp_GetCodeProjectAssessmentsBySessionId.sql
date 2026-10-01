IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeProjectAssessmentsBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeProjectAssessmentsBySessionId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetCodeProjectAssessmentsBySessionId
    @SessionId varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ca.id, ca.SessionId, ca.QuestionId, ca.QuestionText, ca.CodeLanguage, ca.SubmittedCode,
           ca.TimeLimitInMinutes, ca.TimeStarted, ca.TimeCompleted, ca.TimeTakenInSeconds, ca.IsTimeExpired,
           ca.VMPropertyId, ca.RequirementsPassed, ca.RequirementsTotal, ca.ResultsJson
    FROM CodingAssessment ca
    INNER JOIN CodeProjectDetails cpd ON cpd.QuestionId = ca.QuestionId
    WHERE ca.SessionId = @SessionId
    ORDER BY ca.id;
END
GO
