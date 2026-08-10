IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEvaluatorSelectabilityReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEvaluatorSelectabilityReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEvaluatorSelectabilityReport]
    @CandidateId INT,
    @IRFId       INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sr.*,
        sr.Type AS Name,
        lv.[Name] AS RequiredLevel
    FROM dbo.SelectabilityReport sr
    INNER JOIN dbo.[Level] lv ON sr.RequiredLevelId = lv.id
    WHERE sr.CandidateId = @CandidateId
      AND sr.IRFId = @IRFId;
END
GO
