IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchParticipantZoomMeetingWaiting'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchParticipantZoomMeetingWaiting]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
        
          
        
CREATE Procedure usp_FetchParticipantZoomMeetingWaiting        
AS        
BEGIN        
        
DECLARE @MeetingNotificationDetails TABLE(        
        Meetingid bigINT,        
        candidateEmail VARCHAR(150),        
        interviewer VARCHAR(150),        
  risebirdHR Varchar(150),        
  emergencyinterviewer Varchar(max),        
  isInterviewerWaiting bit,      
  EmailType varchar(15),    
  zoomlink varchar(max),    
  CalendlyLink varchar(500),    
  HRCoordinatorEmailId varchar(100),
  candidateName varchar(200)
)       
DECLARE @MeetingNotificationEI TABLE(        
        Meetingid bigINT,        
        candidateEmail VARCHAR(150),        
        interviewer VARCHAR(150),        
  risebirdHR Varchar(150),        
  emergencyinterviewer Varchar(max),        
  isInterviewerWaiting bit,      
  EmailType varchar(15),    
  zoomlink varchar(max),    
  CalendlyLink varchar(500),    
  HRCoordinatorEmailId varchar(100),
  candidateName varchar(200)
)        
DECLARE @MeetingNotificationReschedule TABLE(        
        Meetingid bigINT,        
        candidateEmail VARCHAR(150),        
        interviewer VARCHAR(150),        
  risebirdHR Varchar(150),        
  emergencyinterviewer Varchar(max),        
  isInterviewerWaiting bit,      
  EmailType varchar(15),    
  zoomlink varchar(max),    
  CalendlyLink varchar(500),    
  HRCoordinatorEmailId varchar(100),
  candidateName varchar(200)
)        
DECLARE @MeetingNotificationCandidate TABLE(        
        Meetingid bigINT,        
        candidateEmail VARCHAR(150),        
        interviewer VARCHAR(150),        
  risebirdHR Varchar(150),        
  emergencyinterviewer Varchar(max),        
  isInterviewerWaiting bit,      
  EmailType varchar(15),    
  zoomlink varchar(max),    
  CalendlyLink varchar(500),    
  HRCoordinatorEmailId varchar(100),
  candidateName varchar(200)
)    

;with CTE(Meetingid,candidateEmail,interviewer,risebirdHR,emergencyinterviewer,isInterviewerWaiting,EmailType,zoomLink,CalendlyLink,HRCoordinatorEmailId,candidateName) as (        
Select distinct t1.Meetingid,im.candidateEmail,im.interviewer,ulr.EmailId as risebirdHR,'' as emergencyinterviewer,     
(CASE WHEN (ip.NickName is null or ip.NickName='') THEN 0 ELSE 1 END) as isInterviewerWaiting,'reaminder' as EmailType,im.zoomLink,IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where  
Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1            
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid        
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid        
join IRF on IRF.IRFId=im.irfid  
left join UserList ulr on IRF.risebirdHR=ulr.id
left join TransEmergencyInterviewer tei on tei.IRFId=IRF.id        
left join ZoomMeetingTransmission zmt on zmt.Meetingid=t1.Meetingid and zmt.Event='PARTICIPANT_JOINED' and Event!='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'     
left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName        
where (Isnull(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and IsNull(im.IsMeetingNotificationSent,0)!=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())
)        
Insert Into @MeetingNotificationDetails Select * from CTE        
Update InterviewMeetings set IsMeetingNotificationSent=1,NotificationSentDate=GetDate() where ZoomMeetingId in (Select Meetingid from @MeetingNotificationDetails)        
      
      
Insert into @MeetingNotificationEI      
Select distinct im.ZoomMeetingId as Meetingid,im.candidateEmail,im.interviewer,ulr.EmailId as risebirdHR,ei.emergencyinterviewer,      
(CASE WHEN (ip.NickName is null or ip.NickName='') THEN 0 ELSE 1 END) as isInterviewerWaiting,'EI' as EmailType,im.zoomLink,IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group       
by meetingid) as t1      
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid        
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid      
left join IRF on IRF.IRFId=im.irfid   
left join UserList ulr on IRF.risebirdHR=ulr.id
Outer Apply (select distinct STUFF((SELECT distinct '; ' + CONVERT(varchar(150),ul.EmailId) as Num2         
from TransEmergencyInterviewer t1 join Userlist ul on ul.id=t1.UserId        
where t.IRFId = t1.IRFId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'') emergencyinterviewer         
from TransEmergencyInterviewer t        
where t.IRFId=IRF.id) ei        
left join ZoomMeetingTransmission zmt on zmt.Meetingid=im.ZoomMeetingId and zmt.Event='PARTICIPANT_JOINED' and Event!='PARTICIPANT_LEFT'  and ParticipantName not like '%risebird.io'           
left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName        
where (Isnull(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1  and getdate() > dateadd(minute,5,im.NotificationSentDate) and getdate() < dateadd(minute,20,im.NotificationSentDate) and IsNull(im.IsMeetingNotificationSent,0)=1  and ISNULL(im.IsNotificationSentToEI,0)!=1 and (ip.NickName is null or ip.NickName='')      
      
Update InterviewMeetings set IsNotificationSentToEI=1 where ZoomMeetingId in (Select Meetingid from @MeetingNotificationEI)        

Insert into @MeetingNotificationCandidate      
Select distinct im.ZoomMeetingId as Meetingid,im.candidateEmail,im.interviewer,ulr.EmailId as risebirdHR,ei.emergencyinterviewer,      
(CASE WHEN (ip.NickName is null or ip.NickName='') THEN 0 ELSE 1 END) as isInterviewerWaiting,'CandidateApology' as EmailType,im.zoomLink,IRF.CalendlyLink,im.HRCoordinatorEmailId,im.candidateName from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group       
by meetingid) as t1      
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid        
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid      
left join IRF on IRF.IRFId=im.irfid   
left join UserList ulr on IRF.risebirdHR=ulr.id
Outer Apply (select distinct STUFF((SELECT distinct '; ' + CONVERT(varchar(150),ul.EmailId) as Num2         
from TransEmergencyInterviewer t1 join Userlist ul on ul.id=t1.UserId        
where t.IRFId = t1.IRFId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'') emergencyinterviewer         
from TransEmergencyInterviewer t        
where t.IRFId=IRF.id) ei        
left join ZoomMeetingTransmission zmt on zmt.Meetingid=im.ZoomMeetingId and zmt.Event='PARTICIPANT_JOINED' and Event!='PARTICIPANT_LEFT'  and ParticipantName not like '%risebird.io'           
left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName        
where (Isnull(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1  and getdate() > dateadd(minute,8,im.NotificationSentDate) and getdate() < dateadd(minute,20,im.NotificationSentDate) and IsNull(im.IsMeetingNotificationSent,0)=1  and ISNULL(im.IsNotificationSentToEI,0)=1 and ISNULL(im.IsNotificationSentToCandidate,0)!=1 and (ip.NickName is null or ip.NickName='')      
      
Update InterviewMeetings set IsNotificationSentToCandidate=1 where ZoomMeetingId in (Select Meetingid from @MeetingNotificationEI)
      
Insert into @MeetingNotificationReschedule      
Select distinct im.ZoomMeetingId as Meetingid,im.candidateEmail,im.interviewer,ulr.EmailId as risebirdHR,ei.emergencyinterviewer,      
(CASE WHEN (ip.NickName is null or ip.NickName='') THEN 0 ELSE 1 END) as isInterviewerWaiting,'Reschedule' as EmailType,im.zoomLink,IRF.CalendlyLink,im.HRCoordinatorEmailId,candidateName from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1      
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid        
join InterviewMeetings im on im.ZoomMeetingId=t1.Meetingid      
left join IRF on IRF.IRFId=im.irfid  
left join UserList ulr on IRF.risebirdHR=ulr.id
Outer Apply (select distinct STUFF((SELECT distinct '; ' + CONVERT(varchar(150),ul.EmailId) as Num2         
from TransEmergencyInterviewer t1 join Userlist ul on ul.id=t1.UserId        
where t.IRFId = t1.IRFId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'') emergencyinterviewer         
from TransEmergencyInterviewer t        
where t.IRFId=IRF.id) ei        
left join ZoomMeetingTransmission zmt on zmt.Meetingid=im.ZoomMeetingId and zmt.Event='PARTICIPANT_JOINED' and Event!='PARTICIPANT_LEFT'        
left join InterviewerProfile ip on ip.NickName = zmt.ParticipantName        
where (Isnull(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1  and getdate() > dateadd(minute,15,im.NotificationSentDate) and getdate() < dateadd(minute,20,im.NotificationSentDate) and ISNULL(im.IsMeetingNotificationSent,0)=1 and ISNULL(im.IsNotificationSentToEI,0)=1 and ISNULL(IsRescheduleNotificationSent,0)!=1 and (ip.NickName is null or ip.NickName='')      
      
Update InterviewMeetings set IsRescheduleNotificationSent=1 where ZoomMeetingId in (Select Meetingid from @MeetingNotificationReschedule)        

     
Select * from @MeetingNotificationDetails      
Union      
Select * from @MeetingNotificationEI      
Union      
Select * from @MeetingNotificationCandidate  
Union      
Select * from @MeetingNotificationReschedule      
      
END      
      
GO      
    
    
        
          
        
        
          
        
        
          
        
 
      
      
    
  
      
      
        
            
      
        
             
    
        
          
        
        
          
        
    
      
      
        
        
      
      
        
       
  
  
    
   

GO