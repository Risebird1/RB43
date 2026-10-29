IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchParticipantZoomMeetingWaiting'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchParticipantZoomMeetingWaiting]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO       
Create Procedure usp_FetchParticipantZoomMeetingWaiting                                
AS                                
BEGIN                                
                                
DECLARE @MeetingNotificationDetails TABLE(          
  interviewMeetingId int,        
        Meetingid varchar(200),                                
        candidateEmail VARCHAR(150),                                
        interviewer VARCHAR(150),                                
  risebirdHR Varchar(150),                                
  emergencyinterviewer Varchar(max),                                
  isInterviewerWaiting bit,                              
  EmailType varchar(15),                            
  zoomlink varchar(max),                            
  CalendlyLink varchar(500),                            
  HRCoordinatorEmailId varchar(100),                        
  candidateName varchar(200),                      
    InterviewDate DateTime,                      
  ClientHrContact varchar(200),                      
  ClientName varchar(100),                      
  PrimarySkill varchar(1000),                
  irfId int,        
  LogoLink varchar(1000) ,      
  candidateUniqueId varchar(50),      
  JDLink varchar(2000)      
)                               
DECLARE @MeetingNotificationEI TABLE(          
  interviewMeetingId int,        
        Meetingid varchar(200),                                
        candidateEmail VARCHAR(150),                                
        interviewer VARCHAR(150),                                
  risebirdHR Varchar(150),                                
  emergencyinterviewer Varchar(max),                                
  isInterviewerWaiting bit,                              
  EmailType varchar(15),                            
  zoomlink varchar(max),                            
  CalendlyLink varchar(500),                            
  HRCoordinatorEmailId varchar(100),                        
  candidateName varchar(200),                      
   InterviewDate DateTime,                      
  ClientHrContact varchar(200),                      
   ClientName varchar(100),                      
  PrimarySkill varchar(1000),                
  irfId int,        
  LogoLink varchar(1000),       
  candidateUniqueId varchar(50),      
  JDLink varchar(2000)      
)                                
DECLARE @MeetingNotificationReschedule TABLE(             
  interviewMeetingId int,        
        Meetingid varchar(200),                                
        candidateEmail VARCHAR(150),                                
        interviewer VARCHAR(150),                                
  risebirdHR Varchar(150),                                
  emergencyinterviewer Varchar(max),                                
  isInterviewerWaiting bit,                              
  EmailType varchar(15),                            
  zoomlink varchar(max),                            
  CalendlyLink varchar(500),                            
  HRCoordinatorEmailId varchar(100),                        
  candidateName varchar(200),                      
   InterviewDate DateTime,                      
  ClientHrContact varchar(200),                      
   ClientName varchar(100),                      
  PrimarySkill varchar(1000),                
  irfId int,        
  LogoLink varchar(1000),      
  candidateUniqueId varchar(50),      
  JDLink varchar(2000)      
)                                
DECLARE @MeetingNotificationCandidate TABLE(          
  interviewMeetingId int,        
        Meetingid varchar(200),                                
        candidateEmail VARCHAR(150),                                
        interviewer VARCHAR(150),                                
  risebirdHR Varchar(150),                                
  emergencyinterviewer Varchar(max),                                
  isInterviewerWaiting bit,                              
  EmailType varchar(15),                            
  zoomlink varchar(max),   
  CalendlyLink varchar(500),                            
  HRCoordinatorEmailId varchar(100),                        
  candidateName varchar(200),      
    InterviewDate DateTime,                      
  ClientHrContact varchar(200),                      
  ClientName varchar(100),                      
  PrimarySkill varchar(1000),                
  irfId int,        
  LogoLink varchar(1000),      
  candidateUniqueId varchar(50),      
  JDLink varchar(2000)      
)              
          
--create table #ZoomMeetingTransmissionTempTbl( ParticipantName varchar(200), Meetingid varchar(200))          
          
--insert into #ZoomMeetingTransmissionTempTbl          
--Select ParticipantName, Meetingid from ZoomMeetingTransmission zmt           
--join InterviewMeetings im on im.ZoomMeetingId = zmt.Meetingid          
--where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_JOINED'                    
--EXCEPT           
--Select ParticipantName,Meetingid from ZoomMeetingTransmission zmt          
--join InterviewMeetings im on im.ZoomMeetingId = zmt.Meetingid          
--where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_LEFT'          
          
                        
;with CTE(interviewMeetingId, Meetingid,candidateEmail,interviewer,risebirdHR,emergencyinterviewer,isInterviewerWaiting,      
EmailType,zoomLink,CalendlyLink,HRCoordinatorEmailId,candidateName,interviewDate,ClientHrContact,ClientName,PrimarySkill,       
irfId,LogoLink, candidateUniqueId, JDLink) as (                              
          
Select distinct im.id, t1.Meetingid,im.candidateEmail,CASE WHEN ISNULL(uli.IsActive, 0) = 1 THEN im.interviewer ELSE NULL END as interviewer,ulr.EmailId as risebirdHR,'' as emergencyinterviewer,                             
(CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END) as isInterviewerWaiting, 'reaminder' as EmailType,im.zoomLink,      
IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName,im.interviewDate,im.ClientHrContact,            
Cl.Name as ClientName,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,        
IRF.id as irfId,cl.LogoLink, ulc.uniqueid, irf.JDLink from          
(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where                          
Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                                    
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT'       
and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid                                
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid                        
join IRF on IRF.IRFId=im.irfid                      
left join Client cl on cl.id=irf.Clientid                      
left join UserList ulr on IRF.risebirdHR=ulr.id                        
left join TransInterviewer tei on tei.IRFId=IRF.id            
--left join #ZoomMeetingTransmissionTempTbl zmt on zmt.Meetingid = im.ZoomMeetingId          
Outer Apply(Select Max(Joinedon) as Joinedon From InterviewMeetingsJoinTime imjt where im.id=imjt.interviewid) imj      
Outer Apply(Select Max(PerformedOn) as PerformedOn From PageClickDetails pct where im.id=pct.interviewid and ButtonAction='JoinMeeting' ) pct      
--Outer Apply(Select Max(PerformedOn) as PerformedOn From PageClickDetails pd where pd.Interviewid=im.id and ButtonAction='JoinMeeting') pc          
--Outer Apply (Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_JOINED'                    
--EXCEPT Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_LEFT')zmt                    
--left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName        
left join UserList ulc on ulc.EmailId = im.candidateEmail
left join UserList uli on uli.EmailId = im.interviewer      
where ISNULL(IRF.IsAIInterview,0)=0 and im.Confirmationstatus=1 and ISNULL(Case When IRF.IsCandidateavailability is null and       
IRF.IsMeetingNotificationSend is null then null Else IRF.IsMeetingNotificationSend end,cl.IsMeetingNotificationSend)=1        
and IsNull(im.IsMeetingNotificationSent,0)!=1 and      
dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and       
dateadd(minute,30,im.Interviewdate)>=dateadd(minute,330,getdate()) and  
((Case When (imj.Joinedon is null and pct.PerformedOn is null) then 0 When (imj.Joinedon is not null and pct.PerformedOn is not null) then 0 else 1 end)=1))                                
  
Insert Into @MeetingNotificationDetails Select * from CTE                                
Update InterviewMeetings set IsMeetingNotificationSent=1,NotificationSentDate=GetDate() where ZoomMeetingId in (Select Meetingid from @MeetingNotificationDetails)                                
                              
                              
Insert into @MeetingNotificationEI                              
Select distinct im.id, im.ZoomMeetingId as Meetingid,im.candidateEmail,im.interviewer,ulr.EmailId as risebirdHR,      
ei.emergencyinterviewer, (CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END) as isInterviewerWaiting,'EI' as EmailType,      
im.zoomLink,IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName,im.interviewDate,im.ClientHrContact,      
Cl.Name as ClientName,Case when irf.SkillSetsevaluate>1 and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill, IRF.id as irfId,cl.LogoLink, ulc.uniqueid, irf.JDLink      
from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                              
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid                                
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid                              
left join IRF on IRF.IRFId=im.irfid                           
left join Client cl on cl.id=irf.Clientid                      
left join UserList ulr on IRF.risebirdHR=ulr.id                        
Outer Apply (select distinct STUFF((SELECT distinct '; ' + CONVERT(varchar(150),ul.EmailId) as Num2                                 
from TransInterviewer t1 join Userlist ul on ul.id=t1.UserId                                
where t.IRFId = t1.IRFId and t1.isactive=1 and ul.isactive=1 and ul.EmailId!=im.interviewer FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'') emergencyinterviewer                                 
from TransInterviewer t                                
where t.IRFId=IRF.id) ei             
--left join #ZoomMeetingTransmissionTempTbl zmt on zmt.Meetingid = im.ZoomMeetingId         
Outer Apply(Select Max(Joinedon) as Joinedon From InterviewMeetingsJoinTime imjt where im.id=imjt.interviewid) imj          
--Outer Apply (Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_JOINED'                    
--EXCEPT Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_LEFT')zmt                    
left join UserList ulc on ulc.EmailId  = im.candidateEmail      
--left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName                         
where ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsMeetingNotificationSend end,cl.IsMeetingNotificationSend)=1    
--and (Isnull(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1    
and getdate() > dateadd(minute,3,im.NotificationSentDate) and getdate() < dateadd(minute,19,im.NotificationSentDate) and IsNull(im.IsMeetingNotificationSent,0)=1  and ISNULL(im.IsNotificationSentToEI,0)!=1 and (CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END)=0        
              
                              
Update InterviewMeetings set IsNotificationSentToEI=1 where ZoomMeetingId in (Select Meetingid from @MeetingNotificationEI)                                
                        
Insert into @MeetingNotificationCandidate                              
Select distinct im.id, im.ZoomMeetingId as Meetingid,im.candidateEmail,im.interviewer,ulr.EmailId as risebirdHR,      
ei.emergencyinterviewer, (CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END) as isInterviewerWaiting,'CApology' as EmailType,      
im.zoomLink,IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName,im.interviewDate,im.ClientHrContact,Cl.Name as ClientName,      
Case when irf.SkillSetsevaluate>1 and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill, IRF.id as irfId,cl.LogoLink, ulc.uniqueid, irf.JDLink       
from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group  by meetingid) as t1                              
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid                                
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid                              
left join IRF on IRF.IRFId=im.irfid                           
left join Client cl on cl.id=irf.Clientid                      
left join UserList ulr on IRF.risebirdHR=ulr.id                        
Outer Apply (select distinct STUFF((SELECT distinct '; ' + CONVERT(varchar(150),ul.EmailId) as Num2                                 
from TransInterviewer t1 join Userlist ul on ul.id=t1.UserId                                
where t.IRFId = t1.IRFId and t1.isactive=1 and ul.isactive=1 FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'') emergencyinterviewer                                 
from TransInterviewer t                                
where t.IRFId=IRF.id) ei           
--left join #ZoomMeetingTransmissionTempTbl zmt on zmt.Meetingid = im.ZoomMeetingId          
Outer Apply(Select Max(Joinedon) as Joinedon From InterviewMeetingsJoinTime imjt where im.id=imjt.interviewid) imj          
--outer Apply (Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_JOINED'                    
--EXCEPT Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_LEFT')zmt                                  
left join UserList ulc on ulc.EmailId  = im.candidateEmail      
--left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName                                
where ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsMeetingNotificationSend end,cl.IsMeetingNotificationSend)=1    
--and (Isnull(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1    
and getdate() > dateadd(minute,7,im.NotificationSentDate) and getdate() < dateadd(minute,19,im.NotificationSentDate) and IsNull(im.IsMeetingNotificationSent,0)=1  and ISNULL(im.IsNotificationSentToEI,0)=1 and ISNULL(im.IsNotificationSentToCandidate,0)!=1 
and (CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END)=0        
                              
Update InterviewMeetings set IsNotificationSentToCandidate=1 where ZoomMeetingId in (Select Meetingid from @MeetingNotificationCandidate)                        
                              
Insert into @MeetingNotificationReschedule                              
Select distinct im.id, im.ZoomMeetingId as Meetingid,im.candidateEmail,im.interviewer,ulr.EmailId as risebirdHR,      
ei.emergencyinterviewer, (CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END) as isInterviewerWaiting,'Reschedule' as EmailType,      
im.zoomLink,IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName,im.interviewDate,im.ClientHrContact,Cl.Name as ClientName,      
Case when irf.SkillSetsevaluate>1 and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill, IRF.id as irfId,cl.LogoLink, ulc.uniqueid, irf.JDLink      
from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                              
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid                                
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid                              
left join IRF on IRF.IRFId=im.irfid                        
left join Client cl on cl.id=irf.Clientid                      
left join UserList ulr on IRF.risebirdHR=ulr.id                        
Outer Apply (select distinct STUFF((SELECT distinct '; ' + CONVERT(varchar(150),ul.EmailId) as Num2                                 
from TransInterviewer t1 join Userlist ul on ul.id=t1.UserId                                
where t.IRFId = t1.IRFId and t1.isactive=1 and ul.isactive=1 FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'') emergencyinterviewer                                 
from TransInterviewer t                                
where t.IRFId=IRF.id) ei           
--left join #ZoomMeetingTransmissionTempTbl zmt on zmt.Meetingid = im.ZoomMeetingId          
Outer Apply(Select Max(Joinedon) as Joinedon From InterviewMeetingsJoinTime imjt where im.id=imjt.interviewid) imj          
--outer Apply (Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_JOINED'                    
--EXCEPT Select ParticipantName,Meetingid from ZoomMeetingTransmission where ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId and Event='PARTICIPANT_LEFT')zmt                    
left join UserList ulc on ulc.EmailId  = im.candidateEmail      
--left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName                                
where ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsMeetingNotificationSend end,cl.IsMeetingNotificationSend)=1    
--and (Isnull(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1    
and getdate() > dateadd(minute,14,im.NotificationSentDate) and getdate() < dateadd(minute,19,im.NotificationSentDate) and ISNULL(im.IsMeetingNotificationSent,0)=1 and ISNULL(im.IsNotificationSentToEI,0)=1 and ISNULL(IsRescheduleNotificationSent,0)!=1 and 
(CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END)=0        
                              
Update InterviewMeetings set IsRescheduleNotificationSent=1 where ZoomMeetingId in (Select Meetingid from @MeetingNotificationReschedule)

-- Second waiting alert (WhatsApp only): 5 to 30 minutes after start, the first alert went out
-- and the same person is still the only one in the meeting.
DECLARE @MeetingNotificationWaitingFinal TABLE(
  interviewMeetingId int,
        Meetingid varchar(200),
        candidateEmail VARCHAR(150),
        interviewer VARCHAR(150),
  risebirdHR Varchar(150),
  emergencyinterviewer Varchar(max),
  isInterviewerWaiting bit,
  EmailType varchar(15),
  zoomlink varchar(max),
  CalendlyLink varchar(500),
  HRCoordinatorEmailId varchar(100),
  candidateName varchar(200),
    InterviewDate DateTime,
  ClientHrContact varchar(200),
  ClientName varchar(100),
  PrimarySkill varchar(1000),
  irfId int,
  LogoLink varchar(1000),
  candidateUniqueId varchar(50),
  JDLink varchar(2000)
)

Insert into @MeetingNotificationWaitingFinal
Select distinct im.id, im.ZoomMeetingId as Meetingid,im.candidateEmail,CASE WHEN ISNULL(uli.IsActive, 0) = 1 THEN im.interviewer ELSE NULL END as interviewer,ulr.EmailId as risebirdHR,'' as emergencyinterviewer,
(CASE WHEN (imj.Joinedon is null) THEN 0 ELSE 1 END) as isInterviewerWaiting, 'WaitingFinal' as EmailType,im.zoomLink,
IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName,im.interviewDate,im.ClientHrContact,
Cl.Name as ClientName,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,
IRF.id as irfId,cl.LogoLink, ulc.uniqueid, irf.JDLink
from InterviewMeetings im
join IRF on IRF.IRFId=im.irfid
left join Client cl on cl.id=irf.Clientid
left join UserList ulr on IRF.risebirdHR=ulr.id
Outer Apply(Select Max(Joinedon) as Joinedon From InterviewMeetingsJoinTime imjt where im.id=imjt.interviewid) imj
Outer Apply(Select Max(PerformedOn) as PerformedOn From PageClickDetails pct where im.id=pct.interviewid and ButtonAction='JoinMeeting' ) pct
left join UserList ulc on ulc.EmailId = im.candidateEmail
left join UserList uli on uli.EmailId = im.interviewer
where ISNULL(IRF.IsAIInterview,0)=0 and im.Confirmationstatus=1
and ISNULL(im.IsMeetingNotificationSent,0)=1 and ISNULL(im.IsWaitingFinalAlertSent,0)!=1 and
dateadd(minute,5,im.Interviewdate)<=dateadd(minute,330,getdate()) and
dateadd(minute,30,im.Interviewdate)>dateadd(minute,330,getdate()) and
((Case When (imj.Joinedon is null and pct.PerformedOn is null) then 0 When (imj.Joinedon is not null and pct.PerformedOn is not null) then 0 else 1 end)=1)

Update InterviewMeetings set IsWaitingFinalAlertSent=1 where id in (Select interviewMeetingId from @MeetingNotificationWaitingFinal)


Select * from @MeetingNotificationDetails
Union
Select * from @MeetingNotificationEI
Union
Select * from @MeetingNotificationCandidate
Union
Select * from @MeetingNotificationReschedule
Union
Select * from @MeetingNotificationWaitingFinal
                              
END       
 
GO      