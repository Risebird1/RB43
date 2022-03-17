IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupsByCategory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupsByCategory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAssessmentGroupsByCategory]
@categoryId int

AS
Begin

select id, AssessmentGroup from AssessmentGroup where Categoryid=@categoryId

End
GO


