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

declare @RescheduleInterviewMeetingCount int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid where ulsh.Emailid= @Emailid and confirmationStatus=2 and Cancellationsource like'%Reschedule%' and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE))

 declare @PotentialRiskInterviewCount int = (select Count(im.id) from InterviewMeetings im inner join CandidateFeedbackDetails cfd on im.id=cfd.interviewid where(Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate()))
 declare @SuspeciousInterviewerCount int=(select distinct Count(si.Id) from SuspiciousInterviewer si Join UserList ul on si.UserId=ul.id join InterviewerProfile ip on ul.id=ip.UserId where si.CreatedBy=@Emailid and si.SuspiciousStatus=1)
 
 declare @CancelledInterviewCount int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid where ulsh.Emailid= @Emailid and confirmationStatus=2 and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE)
 and im.RedoInterview=0) 

 declare @RedoInterviewCount int = (select Count(im.id)  from InterviewMeetings im                              
  inner join IRF on im.IRFId = IRF.IRFId                              
  left join UserList ulc on im.candidateEmail= ulc.EmailId 
  join SeniorHr sh on sh.Clientid=irf.ClientId
  join CandidatesMaster cm on ulc.id = cm.UserId                                             
  left join Client cl on cl.id = irf.ClientId                             
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId 
where im.RedoInterview=1 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate())and ulc.Emailid= @Emailid)

 declare @ProbableNoShowCount int=( select COUNT(pnc.id) from ProbableNoShowCandidates pnc  
join InterviewMeetings im on im.id = pnc.InterviewId  
join UserList ul on ul.id = pnc.CandidateUserId  
join InterviewComments ic on ic.Interviewid=im.id where ul.EmailId= @Emailid)

declare @PotentialRiskByClient int=(select Count(ccfr.id) from  ClientCommentsForReport ccfr join InterviewMeetings im on ccfr.InterviewId=im.id join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId          
 left join UserList ulsh on ulsh.id=sh.Userid where ulsh.Emailid= @Emailid and interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3) 

 declare @BlackListCompanyCount int=(select COUNT(Id) from BlackListedCompany where IsBlackList=1)


 declare @interviewerChurnCount int=(select COUNT(Id) from InterviewerProfile where Isavailable in (3,4))

declare @PaymentSlacomplianceCount int=(Select count(ps.id) From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId where ISNull(ps.IsApproved,0)=0)
declare @RedoRequestCount int=(Select COUNT(ri.id) from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id where Approved=0)
declare @HighRiskInterviewers int=(Select Count(ip.Id) from InterviewerProfile ip join UserList ul on ul.id=ip.UserId join InterviewMeetings im on im.interviewer=ul.EmailId where interviewDate>@currentdate and ip.RedMarker>0)

select @UnassignedInterviewsCount as UnassignedInterviewsCount, @UnfilledMeetingStatusCount as UnfilledMeetingStatusCount,
@UnfilledCommentsCount as UnfilledCommentsCount, @MissingZoomLink as MissingZoomLink, @UnAttendedZoomLink as UnAttendedZoomLink, 
@MissingVideoLink as MissingVideoLink, @NotProcessedCandidateResume as NotProcessedCandidateResume,
@QCPendingReportCount as QCPendingReportCount,@NeedClarityCount as NeedClarityCount,@OpenIssueCount as OpenIssueCount,
@RescheduleInterviewMeetingCount as RescheduleInterviewMeetingCount,
@PotentialRiskInterviewCount as PotentialRiskInterviewCount,
@SuspeciousInterviewerCount as SuspeciousInterviewerCount,
@CancelledInterviewCount as CancelledInterviewCount,
@RedoInterviewCount as RedoInterviewCount,
@ProbableNoShowCount as ProbableNoShowCount,
@PotentialRiskByClient as PotentialRiskByClient,
@BlackListCompanyCount as BlackListCompanyCount,
@PaymentSlacomplianceCount as PaymentSlacomplianceCount,
@interviewerChurnCount as InterviewerChurnCount,
@RedoRequestCount as RedoRequestCount,
@HighRiskInterviewers as HighRiskInterviewers
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

set @RescheduleInterviewMeetingCount= (select Count(im.id) from InterviewMeetings im  where confirmationStatus=2 and Cancellationsource like'%Reschedule%' and CAST(interviewDate as DATE) >=CAST(@currentdate as DATE))

set @PotentialRiskInterviewCount = (select Count(im.id) from InterviewMeetings im inner join CandidateFeedbackDetails cfd on im.id=cfd.interviewid where(Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate()))
set @SuspeciousInterviewerCount= (select distinct Count(si.Id) from SuspiciousInterviewer si Join UserList ul on si.UserId=ul.id join InterviewerProfile ip on ul.id=ip.UserId where si.SuspiciousStatus=1)
set @CancelledInterviewCount=(select Count(im.id) from InterviewMeetings im  where confirmationStatus=2  and CAST(interviewDate as DATE) >=CAST(GETDATE() as DATE) and im.RedoInterview=0)

set @RedoInterviewCount=(select Count(im.id)  from InterviewMeetings im                              
  inner join IRF on im.IRFId = IRF.IRFId                              
  left join UserList ulc on im.candidateEmail= ulc.EmailId                                                          
  join CandidatesMaster cm on ulc.id = cm.UserId                                             
  left join Client cl on cl.id = irf.ClientId                             
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId 
where im.RedoInterview=1 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate()))

set  @ProbableNoShowCount =(select COUNT(pnc.id) from ProbableNoShowCandidates pnc  
join InterviewMeetings im on im.id = pnc.InterviewId  
join UserList ul on ul.id = pnc.CandidateUserId  
join InterviewComments ic on ic.Interviewid=im.id)

set @PotentialRiskByClient =(select Count(ccfr.id) from  ClientCommentsForReport ccfr join InterviewMeetings im on ccfr.InterviewId=im.id join irf on irf.IRFId = im.IRFId where interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3)
set @BlackListCompanyCount=(select COUNT(Id) from BlackListedCompany where IsBlackList=1)
set @PaymentSlacomplianceCount =(Select count(ps.id) From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId where ISNull(ps.IsApproved,0)=0)


 set @interviewerChurnCount=(select COUNT(Id) from InterviewerProfile where Isavailable in (3,4))
 set @RedoRequestCount=(Select COUNT(ri.id) from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id where Approved=0)
 set @HighRiskInterviewers =(Select Count(ip.Id) from InterviewerProfile ip join UserList ul on ul.id=ip.UserId join InterviewMeetings im on im.interviewer=ul.EmailId where interviewDate>@currentdate and ip.RedMarker>0)
select @UnassignedInterviewsCount as UnassignedInterviewsCount, @UnfilledMeetingStatusCount as UnfilledMeetingStatusCount,
@UnfilledCommentsCount as UnfilledCommentsCount, @MissingZoomLink as MissingZoomLink, @UnAttendedZoomLink as UnAttendedZoomLink, 
@MissingVideoLink as MissingVideoLink, @NotProcessedCandidateResume as NotProcessedCandidateResume,
@QCPendingReportCount as QCPendingReportCount,@NeedClarityCount as NeedClarityCount,@OpenIssueCount as OpenIssueCount,
@RescheduleInterviewmeetingCount as RescheduleInterviewMeetingCount,
@PotentialRiskInterviewCount as PotentialRiskInterviewCount,
@SuspeciousInterviewerCount as SuspeciousInterviewerCount,
@CancelledInterviewCount as CancelledInterviewCount,
@RedoInterviewCount as RedoInterviewCount,
@ProbableNoShowCount as ProbableNoShowCount,
@PotentialRiskByClient as PotentialRiskByClient,
@BlackListCompanyCount as BlackListCompanyCount,
@PaymentSlacomplianceCount as PaymentSlacomplianceCount,
@interviewerChurnCount as InterviewerChurnCount,
@RedoRequestCount as RedoRequestCount,
@HighRiskInterviewers as HighRiskInterviewers
END        
End
GO
