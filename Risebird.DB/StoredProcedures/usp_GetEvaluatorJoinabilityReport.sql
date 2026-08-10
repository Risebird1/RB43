IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEvaluatorJoinabilityReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEvaluatorJoinabilityReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEvaluatorJoinabilityReport]
    @CandidateId INT,
    @IRFId       INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        jr.*,
        jr.Type AS Name,
        lv.[Name] AS RequiredLevel
    FROM dbo.JoinabilityReport jr
    INNER JOIN dbo.[Level] lv ON jr.RequiredLevelId = lv.id
    WHERE jr.CandidateId = @CandidateId
      AND jr.IRFId = @IRFId;
END
GO
