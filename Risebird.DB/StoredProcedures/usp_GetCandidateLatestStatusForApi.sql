
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateLatestStatusForApi'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateLatestStatusForApi
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidateLatestStatusForApi]    
@skillChannelName varchar(25),     
@candidateEmailId varchar(150)  
AS    
Begin  

declare @IRFId int  = (select id from IRF where IRFId = @skillChannelName)
declare @candidateMasterId int  = (select id from CandidatesMaster where userId = (select id from UserList where EmailId = @candidateEmailId))


  
 select top 1 cs.IRFId, cs.CandidateId, cs.AudioFileLink, cs.Comments,
 DATEADD(minute,330,cs.Date) as Date, cs.Status, cs.PerformedBy, cs.InterviewId,
 csom.StatusValue, ic.MeetingStatus, im.confirmationStatus, im.interviewDate,
 tqr.IsReportBlocked, im.RedoInterview  
 from CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id 
 left join InterviewMeetings im on im.id=cs.InterviewId 
 left join InterviewComments ic on ic.InterviewId=cs.InterviewId
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id
 where cs.IRFId=@IRFId and CandidateId = @candidateMasterId 
 order by cs.[Date] desc
  
End  
GO


