
  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SurveyCandidateEmail'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SurveyCandidateEmail]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
        
		      
CREATE PROCEDURE [dbo].[usp_SurveyCandidateEmail]  
(  
      @SurveyBulkDetails AS [dbo].[CuratedIRFMapping_TableType] READONLY  
)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  
Select im.id as Interviewid,im.candidateEmail as EmailAddress from InterviewMeetings im join @SurveyBulkDetails sb on sb.CuratedGroupId=im.id  
  
END  