IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeProjectStatusesByIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeProjectStatusesByIRF
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetCodeProjectStatusesByIRF
    @IRFId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cpd.QuestionId, cpd.ValidationStatus, cpd.StackImageCode, cpd.EstimatedMinutes
    FROM CodeProjectDetails cpd
    INNER JOIN SkillChannelSampleQuestions q ON q.id = cpd.QuestionId
    WHERE q.IRFId = @IRFId;
END
GO
