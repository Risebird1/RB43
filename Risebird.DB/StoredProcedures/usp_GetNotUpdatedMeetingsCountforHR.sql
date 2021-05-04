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
select (select Count(im.id) from InterviewMeetings im join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id          
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 ) as UnassignedInterviewsCount,                        
(select Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id          
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledMeetingStatusCount,                        
(select Count(im.id) from InterviewMeetings im                 
inner join InterviewComments ic on ic.InterviewId= im.id                 
left join QCFeedbackReport qc on qc.InterviewId = im.id                
join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id          
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and                
(ic.InterviewStatus=2 or ic.InterviewStatus=4 )                
and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null)                
and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledInterviewStatusCount,                        
(select Count(im.id) from InterviewMeetings im  join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id          
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate) as MissingZoomLink,                        
(select Count(im.id) from InterviewMeetings im  join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and im.ZoomMeetingId in  
(Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1      
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid   
where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))) as UnAttendedZoomLink,  
(select Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate) as MissingVideoLink  
End       
    GO
  
              

    
                   
  
  
  
                
  