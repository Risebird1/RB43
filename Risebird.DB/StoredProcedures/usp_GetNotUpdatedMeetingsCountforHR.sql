IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetingsCountforHR'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetingsCountforHR
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetNotUpdatedMeetingsCountforHR]                         
@currentdate datetime,              
@emailID varchar(200)               
AS                                
Begin        
    
declare @hrId int                
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@EmailId and idProfile=2)     
declare @userid int                    
set @userid = (select id from UserList where EmailId=@EmailId)                                    
select (Select Count(*) as Countunion from (select im.id from InterviewMeetings im join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id                
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1     
union    
select im.id from InterviewMeetings im join IRF on irf.irfid=im.IRFId                    
where irf.RisebirdHr =@userid and confirmationStatus = 1 and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1)tb1) as UnassignedInterviewsCount,                              
(Select Count(*) as Countunion from (select im.id from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id                
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7    
union    
select im.id from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id join IRF on irf.irfid=im.IRFId                    
where irf.RisebirdHr =@userid and confirmationStatus = 1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7)tb1) as UnfilledMeetingStatusCount,                              
(Select Count(*) as Countunion from (select im.id from InterviewMeetings im  join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id                
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate    
union    
select im.id from InterviewMeetings im  join IRF on irf.irfid=im.IRFId       
where irf.RisebirdHr =@userid and confirmationStatus = 1 and confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate)tb1) as MissingZoomLink,                              
(select Count(im.id) from InterviewMeetings im  join IRF on irf.irfid=im.IRFId join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus = 1 and ul.EmailId=@emailID and confirmationStatus=1 and im.ZoomMeetingId in        
(Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1            
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid         
where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))) as UnAttendedZoomLink,        
(Select Count(*) as Countunion from (select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId  where confirmationStatus =1 and th.HRId=@hrId and ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and DATEDIFF(DAY, interviewDate, @currentdate)<7
union    
select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join IRF on irf.irfid=im.IRFId where irf.RisebirdHr =@userid and confirmationStatus = 1 and ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and DATEDIFF(DAY, interviewDate, @currentdate)<7)tb1) as MissingVideoLink,
(Select Count(*) as Countunion from (select cr.id from CandidateResume cr join IRF on irf.id=cr.IRFId join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId  where th.HRId=@hrId
union    
select cr.id from CandidateResume cr join IRF on irf.id=cr.IRFId where irf.RisebirdHr =@userid )tb1) as NotProcessedCandidateResume,
(Select Count(*) as Countunion from (Select im.id from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join IRF on IRF.IRFId=im.IRFId
join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id 
where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and th.HRId=@hrId
Union
Select im.id from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join IRF on IRF.IRFId=im.IRFId
join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id 
where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and irf.RisebirdHr =@userid)tb1) as QCPendingReportCount
End     
GO
  
              

    
                   
  
  
  
                
  