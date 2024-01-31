IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSimilarAssessmentGroups'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSimilarAssessmentGroups
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetSimilarAssessmentGroups
@searchValue varchar(150)
as
BEGIN

select id, AssessmentGroup from AssessmentGroup where AssessmentGroup like '%' + @searchValue + '%'

END
Go