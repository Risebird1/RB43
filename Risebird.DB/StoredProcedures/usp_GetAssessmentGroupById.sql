IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupById
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetAssessmentGroupById
@assessmentGroupId int
AS
BEGIN

select AssessmentGroup from AssessmentGroup where id = @assessmentGroupId

END
GO