IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSkillChannelCompletionDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelCompletionDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_GetSkillChannelCompletionDetails         
@irfId int,          
@type varchar(20),      
@timeNow datetime = null      
AS          
BEGIN          
      
--Missed Candidate          
if(@type='MissedCandidate')            
begin            
Select Distinct im.id,IRF.id as irfIntId,im.interviewDate,imj.Joinedon,imjf.FirstJoinedon, ic.MeetingStatus,tzme.Joinedcount as TotalCount,          
im.IsMeetingNotificationSent,im.IsNotificationSentToEI,im.IsRescheduleNotificationSent,im.IsEmergency,im.EIAssigned,imc.PerformedOn,imcf.FirstPerformedOn,          
im.confirmationStatus,im.interviewer,im.candidateEmail,cl.Name as ClientName,im.candidateName,im.MobileNumber,im.IRFId          
,ulc.EmailId as ChannelOwner,IRF.JDTiltle,im.resumeLink,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid      
,iman.Interviewdate as LastInterviewDate,iman.MeetingStatus as LastMeetingStatus,im.ZoomMeetingId      
from InterviewMeetings im                  
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId          
Left join InterviewComments ic on ic.InterviewId=im.id                
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid            
left join UserList ulc on ulc.id=IRF.RisebirdHr          
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj          
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc          
outer apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imjf          
outer apply(Select min(PerformedOn) FirstPerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imcf          
outer Apply(Select top(1)ima.interviewDate,ica.MeetingStatus From InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where im.IRFId=ima.IRFId and im.candidateEmail=ima.candidateEmail and ima.confirmationStatus=1 order by ima.Interviewdate desc) iman      
where IRF.id=@irfId and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)         
and ((im.IsRescheduleNotificationSent=1 and ic.MeetingStatus!=3 and ic.MeetingStatus!=4 and ic.MeetingStatus!=6 and ic.MeetingStatus!=7 and ic.MeetingStatus!=8)        
or ISNULL(imc.PerformedOn,'')!='' and(ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9))        
end      
      
--ProbableNoShow       
else if(@type='ProbableNoShow')          
begin        
select ul.EmailId as candidateEmail, im.interviewDate, im.IRFId, ic.MeetingStatus, ic.InterviewStatus,ul.MobileNumber,uli.EmailId as ChannelOwner      
from ProbableNoShowCandidates pnc        
join InterviewMeetings im on im.id = pnc.InterviewId        
join UserList ul on ul.id = pnc.CandidateUserId        
join InterviewComments ic on ic.Interviewid=im.id      
inner join IRF on im.IRFId=irf.IRFId       
left join UserList uli on uli.id=  irf.RisebirdHr      
where  irf.id =@irfId and im.interviewDate >= @timeNow      
order by im.interviewDate desc      
end       
      
--NotProcessedResumes      
else if(@type='NotProcessedResumes')          
begin      
Select AddedOn, AddedBy, ResumeLink from CandidateResume where IsnUll(Isprocessed,0)=0 and IRFId = @irfId      
end      
--Late Join  
else if(@type='Latejoin')          
begin     
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotalLatejoin    
from IRF ir join InterviewMeetings im on im.IRFId=ir.IRFId    
join InterviewMeetingsJoinTime imj on im.id=imj.interviewid join InterviewComments ic on ic.Interviewid=im.id         
where DATEDIFF(MINUTE,interviewDate,Joinedon)>5 and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9) and ir.id=@irfId    
group by im.IRFId,im.interviewer)    
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,CTE.TotalLatejoin,Count(distinct im.id) TotalCount     
from CTE    
join IRF on IRF.IRFId=CTE.IRFId    
join UserList ul on IRF.RisebirdHr=ul.id    
join Client cl on cl.id=IRF.Clientid    
left join UserList ulc on ulc.id=IRF.BillingOwner    
left join UserList uli on uli.Emailid=CTE.interviewer    
left join InterviewerProfile ip on ip.UserId=uli.id    
left join InterviewMeetings im on im.IRFId=IRF.IRFId and im.interviewer=CTE.interviewer and confirmationStatus=1    
group by IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId,cl.Name,uli.id,ip.CompanyName,ulc.EmailId,IRF.JDTiltle,CTE.TotalLatejoin    
end    
--Unassigned Interviews  
else if(@type='Unassigned')  
begin  
Select distinct im.id,im.interviewDate,im.interviewer,im.candidateEmail from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId   
where confirmationStatus=1 and interviewer is null and IRF.id=@irfId    
end  
--Request for Need Clarity  
else if(@type='NeedClarity')  
begin  
Select distinct im.id,im.interviewDate,im.interviewer,im.candidateEmail,ic.MeetingStatus From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId  
join InterviewComments ic on ic.InterviewId=im.id  
where CandidateResponse=4 and confirmationStatus=1 and IRF.id=@irfId    
end  
--No Show Interviewers  
else if(@type='NoShowInterviewer')  
begin  
Select distinct im.id,im.interviewDate,im.interviewer,im.candidateEmail,ic.MeetingStatus from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId   
join InterviewComments ic on ic.InterviewId=im.id  
where confirmationStatus=1 and IRF.id=@irfId  and (ic.MeetingStatus=5 or ic.MeetingStatus=9)  
end  
END 
GO