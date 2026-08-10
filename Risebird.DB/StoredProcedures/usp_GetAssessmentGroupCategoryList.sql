IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupCategoryList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupCategoryList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAssessmentGroupCategoryList]
AS
BEGIN
    SELECT id, CategoryName AS Name FROM AssessmentGroupCategory
END
GO
