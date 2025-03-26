/****** Object:  StoredProcedure [dbo].[usp_GetNotUpdatedMeetingsCount]    Script Date: 15-01-2021 5.08.54 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetingsCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetingsCount
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetNotUpdatedMeetingsCount]    
@currentdate datetime,            
@Emailid varchar(150)              
AS                                        
Begin        
Declare @Role varchar(150)                  
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid               
      
declare @TechQCCount int =(Select count(id) from TechQualityDetails where isActive=1)    
    
if(@Role='SeniorHR' or @Role = 'HR')--or @Role='SupportAdmin'                  
BEGIN          
declare @UnassignedInterviewsCount int = (select Count(distinct im.id) from InterviewMeetings im     
             join IRF on im.IRFId=irf.IRFId     
                join Client cl on irf.ClientId=cl.id     
    join SeniorHr sh on sh.Clientid=irf.ClientId     
    left join UserList ulsh on ulsh.id=sh.Userid     
    join UserList ul on ul.id= irf.RisebirdHr    
    join AssessmentGroup asg on asg.id = irf.AssessmentGroupId     
    where ul.Emailid= @Emailid and confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 )        
        
declare @UnfilledMeetingStatusCount int = (select Count(distinct im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId                  
 left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id    
 left join UserList uli on uli.EmailId=im.interviewer    
 left join UserList ulc on ulc.id=IRF.RisebirdHr    
 where confirmationStatus=1 and        
 interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulc.Emailid= @Emailid)        
        
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
 left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh
.Emailid= @Emailid)        
        
declare @NotProcessedCandidateResume int = (Select count(cr.id) from CandidateResume cr join irf on irf.id = cr.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId             
 left join UserList ulsh on ulsh.id=sh.Userid join UserList ulc on ulc.id=irf.RisebirdHr where IsnUll(cr.Isprocessed,0)=0 and ulc.Emailid= @Emailid)        
        
declare @QCPendingReportCount int = (Select Count(distinct im.id)  from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join IRF on IRF.IRFId=im.IRFId            
join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id 
 
       
left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1     
left join UserList ulco on IRF.RisebirdHr=ulco.id    
where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ISNULL(im.RedoInterview,0)=0 and ulco.Emailid= @Emailid)        
          
declare @NeedClarityCount int=(select Count(distinct im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId join UserList ulsh on ulsh.id=sh.Userid left join UserList ul on ul.id=  irf.RisebirdHr where  confirmationStatus=1 and CandidateResponse=4      
 and interviewdate>=@currentdate and ulsh.Emailid= @Emailid)        
        
declare @OpenIssueCount int=(Select Count(id) from RaisedIssues where Status='Open' and Requester in('Interviewer','Client'))        
        
declare @RescheduleInterviewMeetingCount int = (select Count(im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId                  
 left join UserList ulsh on ulsh.id=sh.Userid join UserList ul on ul.id=irf.RisebirdHr where ul.Emailid= @Emailid and confirmationStatus=2 and Cancellationsource like'%Reschedule%' and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE))        
        
 declare @PotentialRiskInterviewCount int = (select Count(im.id) from CandidateFeedbackDetails cfd    
inner join InterviewMeetings im  on cfd.interviewid=im.id    
join IRF on im.IRFId=irf.IRFId    
join Client cl on irf.ClientId=cl.id     
join UserList ul on ul.id=  irf.RisebirdHr    
left join UserList uli on im.interviewer= uli.EmailId      
left join InterviewerProfile itp on itp.UserId = uli.id     
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId    
left join PaymentSLADetails ps on im.id=ps.Interviewid    
left join UserList ulc on ulc.EmailId=im.candidateEmail                
join CandidatesMaster cm on cm.UserId=ulc.id     
where cfd.Rating<=3 and Cast(interviewDate as Date)<= GETDATE() and interviewDate>=DATEADD(Day,-30,GETDATE()) AND ul.EmailId=@Emailid)        
  
 declare @SuspeciousInterviewerCount int=(select  Count(distinct si.Id) from SuspiciousInterviewer si Join UserList ul on si.UserId=ul.id join InterviewerProfile ip on ul.id=ip.UserId where  si.SuspiciousStatus=1)        
         
 declare @CancelledInterviewCount int = (select Count(distinct im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId                  
 left join UserList ulsh on ulsh.id=sh.Userid join UserList ul on ul.id=irf.RisebirdHr  where ul.Emailid= @Emailid and confirmationStatus=2 and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE)        
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
join InterviewComments ic on ic.Interviewid=im.id       
inner join IRF on im.IRFId=irf.IRFId       
left join UserList uli on uli.id=  irf.RisebirdHr      
where ul.EmailId= @Emailid)        
        
declare @PotentialRiskByClient int=(select Count(distinct ccfr.id) from     
ClientCommentsForReport ccfr     
join InterviewMeetings im on ccfr.InterviewId=im.id    
     join InterviewComments ic on ic.InterviewId=im.id     
       left join Trans_QCFormReport tq on tq.Interviewid=im.id    
    join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id     
    join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid     
    join UserList ul on ul.id=  irf.RisebirdHr     
    left join PaymentSLADetails ps on im.id=ps.Interviewid    
    left join InterviewerProfile itp on itp.UserId = ul.id    
 where ul.Emailid= @Emailid and interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3)         
        
 declare @BlackListCompanyCount int=(select COUNT(Id) from BlackListedCompany where IsBlackList=1)        
        
        
 declare @interviewerChurnCount int=(select COUNT(Id) from InterviewerProfile where Isavailable in (3,4))        
        
declare @PaymentSlacomplianceCount int=(Select count(ps.id) From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId where ISNull(ps.IsApproved,0)=0)        
declare @RedoRequestCount int=(Select COUNT(ri.id)   from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id      
inner join IRF on im.IRFId=irf.IRFId      
left join UserList ulc on im.candidateEmail= ulc.EmailId       
left join UserList ulco on ulco.id=irf.RisebirdHr    
where ISNULL(Approved,0)=0 and  ISNULL(Rejected,0)=0 and Isnull(IRF.IsPaused,0)=0 and ulco.EmailId=@EmailId and im.interviewDate>DateAdd(Day,-30,Getdate()))        
declare @HighRiskInterviewers int=(Select Count(ip.Id) from InterviewerProfile ip join UserList ul on ul.id=ip.UserId join InterviewMeetings im on im.interviewer=ul.EmailId where interviewDate>@currentdate and ip.RedMarker>0)        
declare @InterviewerVideoKYC int=(Select COUNT(ip.id) from InterviewerProfile ip join UserList ul on ul.id=ip.UserId where isnull(ip.IsVideoApproved,0)=0 and ul.IsActive=1 and ip.VideoLink is not null)      
declare @ProxyCount int=(Select COUNT(pr.id) from ProxyReport pr       
Join InterviewMeetings im on pr.interviewid=im.id      
join IRF on IRF.IRFId=im.IRFId      
join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
join UserList ul on ul.EmailId=im.candidateEmail      
join UserList uli on uli.id= irf.RisebirdHr      
left join PaymentSLADetails tqc on tqc.Interviewid=im.id  and ISNULL(tqc.isQcDone,0)=1      
where pr.isProxyCandidate=1 and( ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7))      
declare @DMApproveCount int=(select COUNT(irf.id) from IRF join Client c on c.id=irf.ClientId join UserList ul on ul.id= irf.RisebirdHr where DMApproved=1 AND ul.EmailId=@Emailid)      
    
declare @MissedCandidateCount int=(Select Count(distinct im.id) from InterviewMeetings im         
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId        
join SeniorHr sh on sh.Clientid=irf.ClientId        
left join UserList ulsh on ulsh.id=sh.Userid        
Left join InterviewComments ic on ic.InterviewId=im.id              
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid        
left join UserList ulc on ulc.id=IRF.RisebirdHr            
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc        
where ulc.Emailid=@Emailid and im.interviewdate>=DateAdd(Day,-7,(CONVERT(VARCHAR(10),@currentdate,23))) and im.interviewdate<=DateAdd(Day,1,(CONVERT(VARCHAR(10),@currentdate,23))) and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission) and im.confirmationStatus=1    
and ((im.IsRescheduleNotificationSent=1 and ic.MeetingStatus!=3 and ic.MeetingStatus!=4 and ic.MeetingStatus!=6 and ic.MeetingStatus!=7 and ic.MeetingStatus!=8)      
or (imc.PerformedOn is not null and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9))))    
      
  set @TechQCCount=(Select Count(tqd.id)From TechQualityDetails tqd join InterviewMeetings im on tqd.Interviewid=im.id    
join InterviewComments ic on ic.Interviewid=im.id    
join IRF on IRF.IRFId=im.IRFId    
join Client cl on cl.id=IRF.Clientid    
left join UserList ul on ul.id=IRF.Risebirdhr    
where tqd.IsActive=1 and ul.EmailId=@EmailId)    
    
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
@HighRiskInterviewers as HighRiskInterviewers ,      
@InterviewerVideoKYC as InterviewerVideoKYC,      
@ProxyCount as ProxyCount,      
@DMApproveCount as DMApproveCount,      
@MissedCandidateCount as MissedCandidateCount,    
@TechQCCount as TechQCCount    
END       
ELSE IF(@Role='TechSupport')    
BEGIN     
set @OpenIssueCount =(Select Count(id) from RaisedIssues where Status='Open' and Requester not in('Interviewer','Client') and RequestType not in('Operation'))     
select @OpenIssueCount as OpenIssueCount    
END    
ELSE                
BEGIN       
IF(@Role='SupportAdmin')    
Begin    
set @MissedCandidateCount =(Select Count(distinct im.id) from InterviewMeetings im         
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId        
join SeniorHr sh on sh.Clientid=irf.ClientId        
left join UserList ulsh on ulsh.id=sh.Userid        
Left join InterviewComments ic on ic.InterviewId=im.id              
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid        
left join UserList ulc on ulc.id=IRF.RisebirdHr         
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc      
where ulsh.EmailId=@Emailid and im.interviewdate>=DateAdd(Day,-7,(CONVERT(VARCHAR(10),@currentdate,23))) and im.interviewdate<=DateAdd(Day,1,(CONVERT(VARCHAR(10),@currentdate,23))) and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission) and im.confirmationStatus=1 and ((im.IsRescheduleNotificationSent=1 and ic.MeetingStatus!=3 and ic.MeetingStatus!=4 and ic.MeetingStatus!=6 and ic.MeetingStatus!=7 and ic.MeetingStatus!=8)      
or (imc.PerformedOn is not null and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9))))    
End    
    
set @MissedCandidateCount =(Select Count(distinct im.id) from InterviewMeetings im         
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId        
join SeniorHr sh on sh.Clientid=irf.ClientId        
left join UserList ulsh on ulsh.id=sh.Userid        
Left join InterviewComments ic on ic.InterviewId=im.id              
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid        
left join UserList ulc on ulc.id=IRF.RisebirdHr         
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc     
where im.interviewdate>=DateAdd(Day,-7,(CONVERT(VARCHAR(10),@currentdate,23))) and im.interviewdate<=DateAdd(Day,1,(CONVERT(VARCHAR(10),@currentdate,23))) and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission) and im.confirmationStatus=1 and ((im.IsRescheduleNotificationSent=1 and ic.MeetingStatus!=3 and ic.MeetingStatus!=4 and ic.MeetingStatus!=6 and ic.MeetingStatus!=7 and ic.MeetingStatus!=8)      
or (imc.PerformedOn is not null and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9))))    
    
 IF(@Role='Admin')    
BEGIN     
set @OpenIssueCount =(Select Count(id) from RaisedIssues where Status='Open')     
END    
Else    
Begin    
set @OpenIssueCount =(Select Count(id) from RaisedIssues where Status='Open' and Requester in('Interviewer','Client'))     
End    
set @UnassignedInterviewsCount  = (select Count(distinct im.id) from InterviewMeetings im  where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 )        
        
set @UnfilledMeetingStatusCount = (select Count(distinct im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2
  
) and DATEDIFF(DAY, interviewDate, @currentdate)<7 )        
        
 set @UnfilledCommentsCount = (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and isAutoCancelled=1 and DATEDIFF(DAY, interviewDate, @currentdate)<7 )        
        
 set @MissingZoomLink = (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate)        
        
 set @UnAttendedZoomLink  = (Select Count(*) from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1                          
left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid                   
join InterviewMeetings im on im.ZoomMeetingid=t1.meetingid                  
where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))        
        
set @MissingVideoLink = (select Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate,
  
    
@currentdate)<7)        
        
set @NotProcessedCandidateResume = (Select count(*) from CandidateResume where IsnUll(Isprocessed,0)=0)        
        
set @QCPendingReportCount = (Select Count(distinct im.id)  from InterviewMeetings im join IRF on im.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=
  
    
uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id       
left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 left join UserList ulco on IRF.RisebirdHr=ulco.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)    
  
   
and tq.id is null and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ISNULL(im.RedoInterview,0)=0)        
        
set @NeedClarityCount =(select Count(distinct im.id) from InterviewMeetings im join irf on irf.IRFId = im.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId join UserList ulsh on ulsh.id=sh.Userid left join UserList ul on ul.id=  irf.RisebirdHr where  confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate)        
        
       
        
set @RescheduleInterviewMeetingCount= (select Count(im.id) from InterviewMeetings im  where confirmationStatus=2 and Cancellationsource like'%Reschedule%' and CAST(interviewDate as DATE) >=CAST(@currentdate as DATE))        
        
set @PotentialRiskInterviewCount = (select Count(im.id) from CandidateFeedbackDetails cfd    
inner join InterviewMeetings im  on cfd.interviewid=im.id    
join IRF on im.IRFId=irf.IRFId    
join Client cl on irf.ClientId=cl.id     
join UserList ul on ul.id=  irf.RisebirdHr    
left join UserList uli on im.interviewer= uli.EmailId      
left join InterviewerProfile itp on itp.UserId = uli.id     
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId    
left join PaymentSLADetails ps on im.id=ps.Interviewid    
left join UserList ulc on ulc.EmailId=im.candidateEmail                
join CandidatesMaster cm on cm.UserId=ulc.id     
where cfd.Rating<=3 and Cast(interviewDate as Date)<= GETDATE() and interviewDate>=DATEADD(Day,-30,GETDATE()))        
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
join InterviewComments ic on ic.Interviewid=im.id      
inner join IRF on im.IRFId=irf.IRFId       
left join UserList uli on uli.id=  irf.RisebirdHr)        
        
set @PotentialRiskByClient =(select Count(distinct ccfr.id) from     
ClientCommentsForReport ccfr     
join InterviewMeetings im on ccfr.InterviewId=im.id    
    join InterviewComments ic on ic.InterviewId=im.id     
       left join Trans_QCFormReport tq on tq.Interviewid=im.id     
    left join PaymentSLADetails ps on ps.Interviewid=im.id    
    join IRF on im.IRFId=irf.IRFId  join Client cl on irf.ClientId=cl.id     
    join UserList ul on ul.id=  irf.RisebirdHr     
    left join InterviewerProfile itp on itp.UserId = ul.id    
where interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3)        
set @BlackListCompanyCount=(select COUNT(Id) from BlackListedCompany where IsBlackList=1)        
set @PaymentSlacomplianceCount =(Select count(ps.id) From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId where ISNull(ps.IsApproved,0)=0)        
        
        
 set @interviewerChurnCount=(select COUNT(Id) from InterviewerProfile where Isavailable in (3,4))        
 set @RedoRequestCount=(Select COUNT(ri.id) from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id join IRF on IRF.IRFId=im.iRFId where ISNULL(Approved,0)=0 and ISNULL(Rejected,0)=0 and Isnull(IRF.IsPaused,0)=0 and im.interviewDate>DateAdd(Day,-30,Getdate()))        
 set @HighRiskInterviewers =(Select Count(ip.Id) from InterviewerProfile ip join UserList ul on ul.id=ip.UserId join InterviewMeetings im on im.interviewer=ul.EmailId where interviewDate>@currentdate and ip.RedMarker>0)       
 set @InterviewerVideoKYC=(Select COUNT(ip.id) from InterviewerProfile ip join UserList ul on ul.id=ip.UserId where isnull(ip.IsVideoApproved,0)=0 and ul.IsActive=1 and ip.VideoLink is not null)       
 set @ProxyCount=(Select COUNT(pr.id) from ProxyReport pr       
Join InterviewMeetings im on pr.interviewid=im.id      
join IRF on IRF.IRFId=im.IRFId      
join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
join UserList ul on ul.EmailId=im.candidateEmail      
join UserList uli on uli.id= irf.RisebirdHr      
left join PaymentSLADetails tqc on tqc.Interviewid=im.id  and ISNULL(tqc.isQcDone,0)=1      
where pr.isProxyCandidate=1 and( ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7))      
set @DMApproveCount=(select COUNT(irf.id) from IRF join Client c on c.id=irf.ClientId join UserList ul on ul.id= irf.RisebirdHr where DMApproved=1)      
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
@HighRiskInterviewers as HighRiskInterviewers,      
@InterviewerVideoKYC as InterviewerVideoKYC,      
@ProxyCount as ProxyCount,      
@DMApproveCount as DMApproveCount,      
@MissedCandidateCount as MissedCandidateCount,    
@TechQCCount as TechQCCount    
END                
End 
GO
