IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetAssessmentGroupDetails
@assessmentGroupId int
AS
BEGIN

select * from AssessmentGroup where id=@assessmentGroupId

END
GO

  
      