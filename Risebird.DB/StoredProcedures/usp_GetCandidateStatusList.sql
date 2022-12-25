/****** Object:  StoredProcedure [dbo].[usp_GetCandidateStatusList]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStatusList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStatusList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidateStatusList]    
@IRFId int,     
@CandidateId int  
AS    
Begin  
  
 select cs.*, csom.StatusValue, csom.IsManuallyDeletable,ic.MeetingStatus,im.confirmationStatus,tqr.IsReportBlocked  from CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id 
 left join InterviewMeetings im on im.id=cs.InterviewId 
 left join InterviewComments ic on ic.InterviewId=cs.InterviewId
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id
 where cs.IRFId=@IRFId and CandidateId = @CandidateId  
  
End  
GO


