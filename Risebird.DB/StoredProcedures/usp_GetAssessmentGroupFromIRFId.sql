IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupFromIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupFromIRFId
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetAssessmentGroupFromIRFId
@irfId int
AS
BEGIN

select AssessmentGroup from AssessmentGroup where id=(select AssessmentGroupId from IRF where id =@irfId)

END
GO