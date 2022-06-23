IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCompletedInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCompletedInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCompletedInterviewMeetings]                          
@startDate datetime,                      
@endDate datetime          
AS               
Begin                                
select distinct im.id imid,im.IRFId, im.interviewDate,im.candidateEmail,im.ClientHrContact, im.interviewer,                
im.candidateName,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,ic.VideoLink,ic.InterviewerComment,          
ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus,ul.EmailId as risebirdHR,  
im.HRCoordinatorEmailId,cl.[Name] as Client,tqc.id as QCPresent  
from InterviewMeetings im                
inner join IRF on im.IRFId = IRF.IRFId   
join USerList uli on uli.EmailId=im.candidateEmail
join  CandidatesMaster cm on cm.UserId=uli.id 
join Candidate ca on ca.CandidateMasterId=cm.id
left join UserList ul on ul.id=irf.RisebirdHr  
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                
left join Client cl on cl.id = irf.ClientId  
left join Trans_QCFormReport tqc on tqc.Interviewid=im.id  
where IsNull(IsProctor,0)=0 and im.interviewDate >= @startDate AND im.interviewDate < @endDate and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)                                             
End



GO

