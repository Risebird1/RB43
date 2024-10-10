/****** Object:  StoredProcedure [dbo].[usp_GetProbableNoShowCandidates]   ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProbableNoShowCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProbableNoShowCandidates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
    
CREATE PROCEDURE [dbo].[usp_GetProbableNoShowCandidates]        
@Emailid varchar(200)
AS        
Begin       
Declare @Clientid int,@Roleid int
Select @Clientid=cu.ClientId,@Roleid=cu.RoleId From ClientUser cu join UserList ul on ul.id=cu.UserId where ul.EmailId=@Emailid
if(@Clientid>0)
Begin
if(@Roleid=1)
begin
  select ul.EmailId as candidateEmail, im.interviewDate, im.IRFId,ic.MeetingStatus,ic.InterviewStatus,ul.MobileNumber,uli.EmailId as ChannelOwner  
from ProbableNoShowCandidates pnc    
join InterviewMeetings im on im.id = pnc.InterviewId    
join UserList ul on ul.id = pnc.CandidateUserId    
join InterviewComments ic on ic.Interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId   
left join UserList uli on uli.id=  irf.RisebirdHr  
join UserList ula on ula.EmailId=im.candidateEmail
Join CandidatesMaster cm on cm.UserId=ula.id
left join Candidate ca on ca.IRFId=IRF.id and ca.CandidateMasterId=cm.id
where ca.ClientHRAttemptContact=@Emailid
order by im.interviewDate desc    
end
else if(@Roleid=4)
begin
select ul.EmailId as candidateEmail, im.interviewDate, im.IRFId,ic.MeetingStatus,ic.InterviewStatus,ul.MobileNumber,uli.EmailId as ChannelOwner  
from ProbableNoShowCandidates pnc    
join InterviewMeetings im on im.id = pnc.InterviewId    
join UserList ul on ul.id = pnc.CandidateUserId    
join InterviewComments ic on ic.Interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId   
left join UserList uli on uli.id=  irf.RisebirdHr  
join UserList ula on ula.EmailId=im.candidateEmail
Join CandidatesMaster cm on cm.UserId=ula.id
left join Candidate ca on ca.IRFId=IRF.id and ca.CandidateMasterId=cm.id
where ca.ProfileSource=@Emailid
end
else if(@Roleid=9)
begin
select ul.EmailId as candidateEmail, im.interviewDate, im.IRFId,ic.MeetingStatus,ic.InterviewStatus,ul.MobileNumber,uli.EmailId as ChannelOwner  
from ProbableNoShowCandidates pnc    
join InterviewMeetings im on im.id = pnc.InterviewId    
join UserList ul on ul.id = pnc.CandidateUserId    
join InterviewComments ic on ic.Interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId   
left join UserList uli on uli.id=  irf.RisebirdHr  
where IRF.ClientId=@Clientid
end
else
begin
select ul.EmailId as candidateEmail, im.interviewDate, im.IRFId,ic.MeetingStatus,ic.InterviewStatus,ul.MobileNumber,uli.EmailId as ChannelOwner  
from ProbableNoShowCandidates pnc    
join InterviewMeetings im on im.id = pnc.InterviewId    
join UserList ul on ul.id = pnc.CandidateUserId    
join InterviewComments ic on ic.Interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId   
left join UserList uli on uli.id=  irf.RisebirdHr  
join TransClientUser tu on tu.IRFId=IRF.id
join UserList ula on ula.id=tu.ClientUserId
where ula.EmailId=@Emailid
end
End
else
Begin
  select ul.EmailId as candidateEmail, im.interviewDate, im.IRFId,ic.MeetingStatus,ic.InterviewStatus,ul.MobileNumber,uli.EmailId as ChannelOwner  
from ProbableNoShowCandidates pnc    
join InterviewMeetings im on im.id = pnc.InterviewId    
join UserList ul on ul.id = pnc.CandidateUserId    
join InterviewComments ic on ic.Interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId   
left join UserList uli on uli.id=  irf.RisebirdHr  
--where interviewDate >= dateadd(DAY, -8, getdate())  
order by im.interviewDate desc    
End
End     
GO

