IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupCategory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupCategory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAssessmentGroupCategory]
@assessmentGroupId int

AS
Begin

select Categoryid from AssessmentGroup where id = @assessmentGroupId

End
GO


