IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ShowRedoRequestLit'))
BEGIN
    DROP PROCEDURE dbo.usp_ShowRedoRequestLit
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_ShowRedoRequestLit    
@EmailId varchar(200)  
AS    
BEGIN    
Declare @Profile varchar(150)   
Select @Profile=pr.Name from UserList ul join Profile pr on pr.id=ul.idProfile where EmailId=@EmailId  
if (@Profile = 'SeniorHR' or @Profile = 'HR')  
Begin  
Select im.*, im.SessionId,IRF.IsAIInterview,case when ulc.LastName is null then ulc.FirstName                    
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                    
  end as candidateName ,    
  ulc.EmailId as candidateEmailId,IRF.id as irfIntId, ri.CreatedBy, ri.Createdon,ulco.EmailId as ChannelOwner    
  from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id    
inner join IRF on im.IRFId=irf.IRFId    
left join UserList ulc on im.candidateEmail= ulc.EmailId     
left join UserList ulco on ulco.id=irf.RisebirdHr  
where ISNULL(Approved,0)=0 and  ISNULL(Rejected,0)=0 and Isnull(IRF.IsPaused,0)=0 and ulco.EmailId=@EmailId  and im.interviewDate>DateAdd(Day,-30,Getdate())
End  
Else  
Begin  
Select im.*,im.SessionId,IRF.IsAIInterview,case when ulc.LastName is null then ulc.FirstName                    
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                    
  end as candidateName ,    
  ulc.EmailId as candidateEmailId,IRF.id as irfIntId, ri.CreatedBy, ri.Createdon,ulco.EmailId as ChannelOwner    
  from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id    
inner join IRF on im.IRFId=irf.IRFId    
left join UserList ulc on im.candidateEmail= ulc.EmailId     
left join UserList ulco on ulco.id=irf.RisebirdHr  
where ISNULL(Approved,0)=0 and  ISNULL(Rejected,0)=0 and Isnull(IRF.IsPaused,0)=0  and im.interviewDate>DateAdd(Day,-30,Getdate())
End  
END    
GO