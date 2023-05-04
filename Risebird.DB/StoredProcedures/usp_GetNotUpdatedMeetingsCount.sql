/****** Object:  StoredProcedure [dbo].[usp_GetNotUpdatedMeetingsCount]    Script Date: 15-01-2021 5.08.54 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetingsCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetingsCount
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetNotUpdatedMeetingsCount]                             
@currentdate datetime,        
@Emailid varchar(150)          
AS                                    
Begin                                   
Declare @Role varchar(150)          
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid          
if(@Role='SeniorHR')--or @Role='SupportAdmin'          
BEGIN  
declare @UnassignedInterviewsCount int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid where ulsh.Emailid= @Emailid and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 )

declare @UnfilledMeetingStatusCount int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and
 interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid)

 declare @UnfilledCommentsCount int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid  where confirmationStatus=1 and isAutoCancelled=1  and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid )

 declare @MissingZoomLink int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid  where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate and ulsh.Emailid= @Emailid)

 declare @UnAttendedZoomLink int = (Select Count(*) from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                  
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid           
join InterviewMeetings im on im.ZoomMeetingid=t1.meetingid          
join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid        
where ulsh.Emailid= @Emailid and (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and 
dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) 
and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))

declare @MissingVideoLink int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid)

declare @NotProcessedCandidateResume int = (Select count(cr.id) from CandidateResume cr join irf on irf.id = cr.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid where IsnUll(cr.Isprocessed,0)=0 and ulsh.Emailid= @Emailid)

declare @QCPendingReportCount int = (Select Count(distinct im.id)  from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join IRF on IRF.IRFId=im.IRFId    
join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id     
where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<Getdate() and DATEDIFF(DAY, interviewDate, Getdate())<7 and ulsh.Emailid= @Emailid)
  
declare @NeedClarityCount int=(select Count(distinct im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId join UserList ulsh on ulsh.id=sh.Userid where  confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate and ulsh.Emailid= @Emailid)

declare @OpenIssueCount int=(Select Count(id) from RaisedIssues where Status='Open')

--select (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
-- left join UserList ulsh on ulsh.id=sh.Userid where ulsh.Emailid= @Emailid and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 ) as UnassignedInterviewsCount,                                  
--(select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
-- left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid ) as UnfilledMeetingStatusCount,                                  
--(select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
-- left join UserList ulsh on ulsh.id=sh.Userid  where confirmationStatus=1 and isAutoCancelled=1  and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid ) as UnfilledCommentsCount,                                  
--(select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
-- left join UserList ulsh on ulsh.id=sh.Userid  where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate and ulsh.Emailid= @Emailid) as MissingZoomLink,                  
--(Select Count(*) from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                  
--left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid           
--join InterviewMeetings im on im.ZoomMeetingid=t1.meetingid          
--join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid        
--where ulsh.Emailid= @Emailid and (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())) as UnAttendedZoomLink,
--(select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
-- left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid) as MissingVideoLink,      
--(Select count(cr.id) from CandidateResume cr join irf on irf.id = cr.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
-- left join UserList ulsh on ulsh.id=sh.Userid where IsnUll(cr.Isprocessed,0)=0 and ulsh.Emailid= @Emailid) as NotProcessedCandidateResume,    
-- (Select Count(*) from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join IRF on IRF.IRFId=im.IRFId    
--join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id     
--where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid) as QCPendingReportCount    



select @UnassignedInterviewsCount as UnassignedInterviewsCount, @UnfilledMeetingStatusCount as UnfilledMeetingStatusCount,
@UnfilledCommentsCount as UnfilledCommentsCount, @MissingZoomLink as MissingZoomLink, @UnAttendedZoomLink as UnAttendedZoomLink, 
@MissingVideoLink as MissingVideoLink, @NotProcessedCandidateResume as NotProcessedCandidateResume, @QCPendingReportCount as QCPendingReportCount,@NeedClarityCount as NeedClarityCount,@OpenIssueCount as OpenIssueCount

END        
ELSE        
BEGIN  

set @UnassignedInterviewsCount  = (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 )

set @UnfilledMeetingStatusCount = (select Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 )

 set @UnfilledCommentsCount = (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and isAutoCancelled=1 and DATEDIFF(DAY, interviewDate, @currentdate)<7 )

 set @MissingZoomLink = (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate)

 set @UnAttendedZoomLink  = (Select Count(*) from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                  
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid           
join InterviewMeetings im on im.ZoomMeetingid=t1.meetingid          
where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))

set @MissingVideoLink = (select Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7)

set @NotProcessedCandidateResume = (Select count(*) from CandidateResume where IsnUll(Isprocessed,0)=0)

set @QCPendingReportCount = (Select Count(distinct im.id)  from InterviewMeetings im join IRF on im.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id   
where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<Getdate() and DATEDIFF(DAY, interviewDate, Getdate())<7)

set @NeedClarityCount =(select Count(distinct im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId join UserList ulsh on ulsh.id=sh.Userid where  confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate)

set @OpenIssueCount =(Select Count(id) from RaisedIssues where Status='Open')

select @UnassignedInterviewsCount as UnassignedInterviewsCount, @UnfilledMeetingStatusCount as UnfilledMeetingStatusCount,
@UnfilledCommentsCount as UnfilledCommentsCount, @MissingZoomLink as MissingZoomLink, @UnAttendedZoomLink as UnAttendedZoomLink, 
@MissingVideoLink as MissingVideoLink, @NotProcessedCandidateResume as NotProcessedCandidateResume, @QCPendingReportCount as QCPendingReportCount,@NeedClarityCount as NeedClarityCount,@OpenIssueCount as OpenIssueCount




--select (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 ) as UnassignedInterviewsCount,                                  
--(select Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledMeetingStatusCount,                                  
--(select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and isAutoCancelled=1 and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledCommentsCount,                                  
--(select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate) as MissingZoomLink,                  
--(Select Count(*) from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                  
--left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid           
--join InterviewMeetings im on im.ZoomMeetingid=t1.meetingid          
--where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())) as UnAttendedZoomLink,              
--(select Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7) as MissingVideoLink,              
--(Select count(*) from CandidateResume where IsnUll(Isprocessed,0)=0) as NotProcessedCandidateResume,    
--(Select Count(*) from InterviewMeetings im join IRF on im.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id   
--where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7) as QCPendingReportCount    
END        
End

GO
