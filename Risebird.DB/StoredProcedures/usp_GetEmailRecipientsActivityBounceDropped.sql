IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailRecipientsActivityBounceDropped'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailRecipientsActivityBounceDropped
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetEmailRecipientsActivityBounceDropped]         
@recipientType varchar(30),    
@startDate datetime = null,    
@endDate datetime = null,    
@timeNow datetime,    
@EmailId varchar(150)    
AS        
Begin     
Declare @latestActivityRowId as Table(  
id int  
)  
  
;WITH CTE AS    
 (SELECT sge.id,    
  ROW_NUMBER() OVER (PARTITION BY email, Split_Message_Id ORDER BY [timestamp] desc) as rn FROM SendgridEventData sge    
  join UserList ul on ul.EmailId = sge.email    
  where ((@startDate is null and @endDate is null and cast(CreatedAt as date) >= cast(DATEADD(DAY, -1, @timeNow) as date)) or    
  (@startDate is not null and cast(CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(CreatedAt as date) <= cast(@endDate as date)))    
  and ([event] = 'bounce' or [event] = 'dropped')    
  and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))    
    
 )   
 Insert into @latestActivityRowId select id from CTE where rn=1  
    
 Declare @Role varchar(150)                  
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid    
if(@Role='Client')  
BEGIN  
Declare @ClientRoleId int  
Select @ClientRoleId=cu.RoleId From ClientUser cu join UserList ul on ul.id=cu.UserId where ul.EmailId=@Emailid  
if(@ClientRoleId=1)  
begin  
select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,    
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],    
  irf.IRFId as SkillChannelName, im.interviewDate,uli.EmailId as ChannelOwner, ca.ResumeLink as ResumeLink,    
  ca.Phone as MobileNumber,irf.CalendlyLink,ulc.EmailId as candidateEmail,    
  case when ulc.LastName is null then ulc.FirstName                      
else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                      
end as candidateName, ca.ClientHRAttemptContact as CandidateClientHR,IRF.EndDate, SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid
  from SentMailDetails smd    
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id    
  join UserList ul on ul.EmailId = sge.email    
  left join IRF on smd.IRFId = irf.id     
  left join InterviewMeetings im on im.id = smd.InterviewId    
  left join UserList ulc on im.candidateEmail= ulc.EmailId    
  left join UserList uli on uli.id=  irf.RisebirdHr    
  join CandidatesMaster cm on ulc.id = cm.UserId                                                           
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId    
  where sge.id in (select id from @latestActivityRowId)  and ca.ClientHRAttemptContact=@EmailId   
  and sge.email != 'hire@risebird.io'    
  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY,-1, @timeNow) as date)) or    
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))    
  --and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))    
  order by smd.CreatedAt desc    
    
end  
else if(@ClientRoleId=4)  
begin  
select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,    
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],    
  irf.IRFId as SkillChannelName, im.interviewDate,uli.EmailId as ChannelOwner, ca.ResumeLink as ResumeLink,    
  ca.Phone as MobileNumber,irf.CalendlyLink,ulc.EmailId as candidateEmail,    
  case when ulc.LastName is null then ulc.FirstName                      
else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                      
end as candidateName, ca.ClientHRAttemptContact as CandidateClientHR,IRF.EndDate, SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid
  from SentMailDetails smd    
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id    
  join UserList ul on ul.EmailId = sge.email    
  left join IRF on smd.IRFId = irf.id     
  left join InterviewMeetings im on im.id = smd.InterviewId    
  left join UserList ulc on im.candidateEmail= ulc.EmailId    
  left join UserList uli on uli.id=  irf.RisebirdHr    
  join CandidatesMaster cm on ulc.id = cm.UserId                                                           
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId    
  where sge.id in (select id from @latestActivityRowId)  and ca.ProfileSource=@EmailId   
  and sge.email != 'hire@risebird.io'    
  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY,-1, @timeNow) as date)) or    
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))    
  --and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))    
  order by smd.CreatedAt desc    
    
end  
else if(@ClientRoleId=9)  
begin  
Declare @Clientid int  
Select @Clientid=cu.ClientId From ClientUser cu join UserList ul on ul.id=cu.UserId where ul.EmailId=@Emailid  
select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,    
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],    
  irf.IRFId as SkillChannelName, im.interviewDate,uli.EmailId as ChannelOwner, ca.ResumeLink as ResumeLink,    
  ca.Phone as MobileNumber,irf.CalendlyLink,ulc.EmailId as candidateEmail,    
  case when ulc.LastName is null then ulc.FirstName                      
else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                      
end as candidateName, ca.ClientHRAttemptContact as CandidateClientHR,IRF.EndDate, SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid
  from SentMailDetails smd    
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id    
  join UserList ul on ul.EmailId = sge.email    
  left join IRF on smd.IRFId = irf.id     
  left join InterviewMeetings im on im.id = smd.InterviewId    
  left join UserList ulc on im.candidateEmail= ulc.EmailId    
  left join UserList uli on uli.id=  irf.RisebirdHr    
  join CandidatesMaster cm on ulc.id = cm.UserId                                                           
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId    
  where sge.id in (select id from @latestActivityRowId)  and IRF.ClientId=@Clientid  
  and sge.email != 'hire@risebird.io'    
  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY,-1, @timeNow) as date)) or    
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))    
  --and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))    
  order by smd.CreatedAt desc    
    
end  
else  
begin  
select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,    
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],    
  irf.IRFId as SkillChannelName, im.interviewDate,uli.EmailId as ChannelOwner, ca.ResumeLink as ResumeLink,    
  ca.Phone as MobileNumber,irf.CalendlyLink,ulc.EmailId as candidateEmail,    
  case when ulc.LastName is null then ulc.FirstName                      
else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                      
end as candidateName, ca.ClientHRAttemptContact as CandidateClientHR,IRF.EndDate, SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid
  from SentMailDetails smd    
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id    
  join UserList ul on ul.EmailId = sge.email    
  left join IRF on smd.IRFId = irf.id     
  left join InterviewMeetings im on im.id = smd.InterviewId    
  left join UserList ulc on im.candidateEmail= ulc.EmailId    
  left join UserList uli on uli.id=  irf.RisebirdHr    
  join CandidatesMaster cm on ulc.id = cm.UserId                                                           
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId   
  join TransClientUser tcu on tcu.IRFId=IRF.id  
  join UserList ulcu on ulcu.id=tcu.ClientUserId  
  where sge.id in (select id from @latestActivityRowId) and ulcu.EmailId=@EmailId  
  and sge.email != 'hire@risebird.io'    
  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY,-1, @timeNow) as date)) or    
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))    
  --and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))    
  order by smd.CreatedAt desc    
    
end  
END  
--if(@Role='SeniorHR' or @Role = 'HR')    
--BEGIN    
--  select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,    
--  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],    
--  irf.IRFId as SkillChannelName, im.interviewDate,uli.EmailId as ChannelOwner, ca.ResumeLink as ResumeLink,    
--  ca.Phone as MobileNumber,irf.CalendlyLink,ulc.EmailId as candidateEmail,    
--  case when ulc.LastName is null then ulc.FirstName                      
--else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                      
--end as candidateName, ca.ClientHRAttemptContact as CandidateClientHR,IRF.EndDate, SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid
--  from SentMailDetails smd    
--  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id    
--  join UserList ul on ul.EmailId = sge.email    
--  left join IRF on smd.IRFId = irf.id     
--  left join InterviewMeetings im on im.id = smd.InterviewId    
--  left join UserList ulc on im.candidateEmail= ulc.EmailId    
--  left join UserList uli on uli.id=  irf.RisebirdHr    
--  join CandidatesMaster cm on ulc.id = cm.UserId                                                           
--  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId    
--  where sge.id in (select id from @latestActivityRowId) and uli.EmailId=@EmailId    
--  and sge.email != 'hire@risebird.io'    
--  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY,-1, @timeNow) as date)) or    
--  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))    
--  --and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))    
--  order by smd.CreatedAt desc    
    
--End    
Else    
Begin    
  select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,    
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],    
  irf.IRFId as SkillChannelName, im.interviewDate,uli.EmailId as ChannelOwner, ca.ResumeLink as ResumeLink,    
  ca.Phone as MobileNumber,irf.CalendlyLink,ulc.EmailId as candidateEmail,    
  case when ulc.LastName is null then ulc.FirstName                      
else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                      
end as candidateName, ca.ClientHRAttemptContact as CandidateClientHR,IRF.EndDate,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid
  from SentMailDetails smd    
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id    
  join UserList ul on ul.EmailId = sge.email    
  left join IRF on smd.IRFId = irf.id     
  left join InterviewMeetings im on im.id = smd.InterviewId    
  left join UserList ulc on im.candidateEmail= ulc.EmailId    
  left join UserList uli on uli.id=  irf.RisebirdHr    
  join CandidatesMaster cm on ulc.id = cm.UserId           
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId    
  where sge.id in (select id from @latestActivityRowId)     
  and sge.email != 'hire@risebird.io'    
  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY,-1, @timeNow) as date)) or    
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))    
  --and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))    
  order by smd.CreatedAt desc    
    
     
End     
End    
Go