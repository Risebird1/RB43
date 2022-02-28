IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_FalgCountForIrfStats]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_FalgCountForIrfStats
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION fn_FalgCountForIrfStats(@id int,  @timeNow datetime)    
RETURNS int
AS    
BEGIN    

DECLARE @IRFID varchar(15)
DECLARE @Flag int=0
Select @IRFID=IRFId From IRF where id=@id
DECLARE @TemporaryCandidateTableVariable TABLE (          
 IRFId int,    
 CandidateMasterId int,    
 UserId int,    
 CandidateLatestStatus varchar(20)    
);       
    
INSERT INTO @TemporaryCandidateTableVariable 
select ca.IRFId, ca.CandidateMasterId, cm.UserId,      
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast('2018-01-01' as date), @timeNow)    
from  Candidate ca     
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)     
join CandidatesMaster cm on cm.id=ca.CandidateMasterId      
where cs.[Status] =12 and ca.IRFId=@id
group by ca.IRFId, ca.CandidateMasterId,cm.UserId    

DECLARE @CompletedInterviews int 
DECLARE @ReScheduledCount int 
DECLARE @FutureScheduledMeetings int 
DECLARE @PendingCount int 
DECLARE @CandidateCount int 
DECLARE @SelectedMeetings int 
DECLARE @ScheduledPercentage int
DECLARE @CompletionRate int
DECLARE @SelectionRate int
Select @CompletedInterviews=count(UserId) from @TemporaryCandidateTableVariable where CandidateLatestStatus in ('SELECTED', 'REJECTED')
select @FutureScheduledMeetings=count(UserId) from @TemporaryCandidateTableVariable where CandidateLatestStatus = 'FUTURESCHEDULE'
Select @ReScheduledCount=count(UserId) from @TemporaryCandidateTableVariable where CandidateLatestStatus = 'RESCHEDULE'
Select @PendingCount=count(UserId) from @TemporaryCandidateTableVariable where CandidateLatestStatus = 'PENDING'
select @CandidateCount=count(UserId) from @TemporaryCandidateTableVariable
Select @SelectedMeetings=count(UserId) from @TemporaryCandidateTableVariable where CandidateLatestStatus = 'SELECTED'
if(@CandidateCount>0)
BEGIN
Select @ScheduledPercentage=(@CompletedInterviews+@ReScheduledCount+@FutureScheduledMeetings+@PendingCount)*100/@CandidateCount
Select @CompletionRate=(@CompletedInterviews*100)/@CandidateCount
END
if(@CompletedInterviews>0)
BEGIN
Select @SelectionRate=(@SelectedMeetings*100)/@CompletedInterviews
END
 
Declare @UnassignedInterviewsCount int
DECLARE @UnfilledMeetingStatusCount int
DECLARE @UnfilledInterviewStatusCount int
DECLARE @UnfilledCommentsCount int
DECLARE @MissingZoomLink int
DECLARE @UnAttendedZoomLink int
DECLARE @MissingVideoLink int

select @UnassignedInterviewsCount=Count(im.id) from InterviewMeetings im where IRFId=@IRFID and confirmationStatus=1 and interviewer is null
select @UnfilledMeetingStatusCount= Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id where IRFId=@IRFID and confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2)
select @UnfilledInterviewStatusCount=Count(im.id) from InterviewMeetings im inner join InterviewComments ic on ic.InterviewId= im.id left join QCFeedbackReport qc on qc.InterviewId = im.id                        
where IRFId=@IRFID and confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4 ) and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null)
select @UnfilledCommentsCount= Count(im.id) from InterviewMeetings im where IRFId=@IRFID and confirmationStatus=1 and isAutoCancelled=1
select @MissingZoomLink=Count(im.id) from InterviewMeetings im where IRFId=@IRFID and confirmationStatus=1 and (zoomLink is null or zoomLink='')
Select @UnAttendedZoomLink=Count(*) from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid         
join InterviewMeetings im on im.ZoomMeetingid=t1.meetingid        
where IRFId=@IRFID and (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())  
select @MissingVideoLink=Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where IRFId=@IRFID and ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='')

if(@ScheduledPercentage<75)
BEGIN
SET @Flag=@Flag+1
END
if(@CompletionRate<60)
BEGIN
SET @Flag=@Flag+1
END
if(@SelectionRate<20)
BEGIN
SET @Flag=@Flag+1
END
if(@UnassignedInterviewsCount>0)
BEGIN
SET @Flag=@Flag+1
END
if(@UnfilledMeetingStatusCount>0)
BEGIN
SET @Flag=@Flag+1
END
if(@UnfilledCommentsCount>0)
BEGIN
SET @Flag=@Flag+1
END
if(@MissingZoomLink>0)
BEGIN
SET @Flag=@Flag+1
END
if(@UnAttendedZoomLink>0)
BEGIN
SET @Flag=@Flag+1
END
if(@MissingVideoLink>0)
BEGIN
SET @Flag=@Flag+1
END
return @Flag    
END    

GO