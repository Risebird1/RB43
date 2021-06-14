IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetingsCountforHR'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetingsCountforHR
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
create PROCEDURE [dbo].[usp_GetNotUpdatedMeetingsCountforHR]                     
@currentdate datetime,          
@emailID varchar(200)           
AS                            
Begin    
--declare @EmailId varchar(100)
--declare @currentdate datetime
--set @currentdate= '2021-06-12'
--set @EmailId ='shaheen_yasmin@risebird.io' 
declare @hrId int            
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@EmailId and idProfile=2) 
declare @userid int                
set @userid = (select id from UserList where EmailId=@EmailId) 
declare @HrCodinatorUnassigned int
declare @OwnerUnassigned int
declare @HrCoordiUnfilled int
declare @OwnerUnfilled int
declare @HrCoordiIntStatus int
declare  @OwnerIntStatus int
declare @HrCoordiZoomLink int
declare  @OwnerZoomLink int
declare @UnAttendedZoomLink int
declare @hrMissingVideo int
declare @OwnerMissingVideo int
set  @HrCodinatorUnassigned =(select Count(im.id) from InterviewMeetings im join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id            
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 )
set @OwnerUnassigned =(select Count(im.id) from InterviewMeetings im join IRF on irf.irfid=im.IRFId                
where irf.RisebirdHr =@userid and confirmationStatus = 1 and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1)
 set @HrCoordiUnfilled = (select Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id            
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7)
 set @OwnerUnfilled=(select Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id join IRF on irf.irfid=im.IRFId                
where irf.RisebirdHr =@userid and confirmationStatus = 1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7)
set @HrCoordiIntStatus =
(select Count(im.id) from InterviewMeetings im                   
inner join InterviewComments ic on ic.InterviewId= im.id                   
left join QCFeedbackReport qc on qc.InterviewId = im.id                  
join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id            
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and                  
(ic.InterviewStatus=2 or ic.InterviewStatus=4 )                  
and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null)                  
and DATEDIFF(DAY, interviewDate, @currentdate)<7)
set @OwnerIntStatus=(select Count(im.id) from InterviewMeetings im                       
inner join InterviewComments ic on ic.InterviewId= im.id                       
left join QCFeedbackReport qc on qc.InterviewId = im.id                      
join IRF on irf.irfid=im.IRFId where irf.RisebirdHr =@userid and confirmationStatus = 1 and confirmationStatus=1 and                     
(ic.InterviewStatus=2 or ic.InterviewStatus=4 ) and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null)                      
and DATEDIFF(DAY, interviewDate, @currentdate)<7)
 set @HrCoordiZoomLink=(select Count(im.id) from InterviewMeetings im  join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id            
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate)
set @OwnerZoomLink=(select Count(im.id) from InterviewMeetings im  join IRF on irf.irfid=im.IRFId   
where irf.RisebirdHr =@userid and confirmationStatus = 1 and confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate)

set @UnAttendedZoomLink = (select Count(im.id) from InterviewMeetings im  join IRF on irf.irfid=im.IRFId join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus = 1 and ul.EmailId=@emailID and confirmationStatus=1 and im.ZoomMeetingId in    
(Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1        
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid     
where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())))
set @hrMissingVideo= (select Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join IRF on irf.irfid=im.IRFId join TransHR th on th.IRFId = irf.id where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId and ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate)
set @OwnerMissingVideo=(select Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join IRF on irf.irfid=im.IRFId where irf.RisebirdHr =@userid and confirmationStatus = 1 and ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate)      
 select (@HrCodinatorUnassigned+@OwnerUnassigned) as UnassignedInterviewsCount,(@HrCoordiUnfilled+@OwnerUnfilled) as UnfilledMeetingStatusCount,                          
  (@HrCoordiIntStatus+@OwnerIntStatus)as UnfilledInterviewStatusCount, (@HrCoordiZoomLink+@OwnerZoomLink) as MissingZoomLink,@UnAttendedZoomLink as UnAttendedZoomLink,(@hrMissingVideo+@OwnerMissingVideo) as MissingVideoLink   




End  

    GO
  
              

    
                   
  
  
  
                
  