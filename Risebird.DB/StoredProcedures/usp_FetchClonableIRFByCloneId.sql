IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchClonableIRFByCloneId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchClonableIRFByCloneId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchClonableIRFByCloneId]
    @id INT
AS
BEGIN
    SELECT
        ir.CompetencyLevelId AS Heuristicsscore,
        ir.CountryType AS JobCountry,
        AssessmentGroupId
    FROM IRFRecommendations ir
    WHERE ir.CloneIRFId = @id
END
GO
