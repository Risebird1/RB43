/****** Object:  StoredProcedure [dbo].[usp_AddUpdateAssessmentGroupSMEInterviewer]    Script Date: 24-06-2020 1.21.59 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateAssessmentGroupSMEInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateAssessmentGroupSMEInterviewer
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_AddUpdateAssessmentGroupSMEInterviewer]  
(  
      @SMEBulkData AS [dbo].[AssessmentGroupInterviewers_TableType] READONLY,
	  @AssessmentGroupId int

)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  DELETE from AssessmentGroupInterviewers where AssessmentGroupId = @AssessmentGroupId
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[AssessmentGroupInterviewers](  
    [AssessmentGroupId],
	[InterviewerProfileId],
	[IsSME],
	[CreatedBy],
	[CreatedOn],
	[ModifiedBy],
	[ModifiedAt])  
  
SELECT * FROM @SMEBulkData  
  
  
END  
GO


