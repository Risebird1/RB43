/****** Object:  StoredProcedure [dbo].[usp_GetCandidateTimeline]    Script Date: 11-12-2020 05.54.02 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStatusDataWithoutCold'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStatusDataWithoutCold
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetCandidateStatusDataWithoutCold]   
@clientId int,                    
@startDate datetime,                    
@endDate datetime                    
AS                      
Begin               
                     
DECLARE @TemporaryCandidateTableVariable TABLE (                        
 IRFId int,                  
 CandidateMasterId int,                  
 UserId int,            
 CandidateLatestStatus varchar(20),          
 InterviewId int null,          
 interviewDate datetime null,          
 ResumeLink varchar(500) NULL,          
 AddedOnDate datetime null,          
 InvitationSentOnDate datetime null,          
 FirstScheduledOnDate datetime null,          
 FirstScheduledOninterviewDate datetime null,          
 completedDate datetime null,        
 PreReportLink varchar(500) NULL,        
 PreReportUsername varchar(50) NULL,        
 PreReportPassword varchar(50) NULL        
);                    
         
INSERT INTO @TemporaryCandidateTableVariable                        
select ca.IRFId, ca.CandidateMasterId, cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate,  ca.ResumeLink          
,Journey.AddedOnDate, Journey.InvitationSentOnDate, Journey.FirstScheduledOnDate, Journey.FirstScheduledOninterviewDate,        
La.completedDate, ca.PreReportLink, ca.PreReportUsername, ca.PreReportPassword                
from  Candidate ca                  
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                  
join CandidatesMaster cm on cm.id=ca.CandidateMasterId        
join IRF irf on ca.IRFId=irf.id        
outer apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(cm.UserId, ca.IRFId, GETDATE()) La          
outer apply dbo.fn_CandidateJourneyImportantMileStoneData(cm.UserId, ca.IRFId) Journey          
where cs.[Status] = 12 and irf.ClientId=177 and  [Date] between @startDate and @endDate-- cast([Date] as date) <=cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)                  
group by ca.IRFId, ca.CandidateMasterId,cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate, ca.ResumeLink          
, Journey.AddedOnDate, Journey.InvitationSentOnDate, Journey.FirstScheduledOnDate, Journey.FirstScheduledOninterviewDate,        
La.completedDate, ca.PreReportLink, ca.PreReportUsername, ca.PreReportPassword              
         
         
----completed          
select im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle, ic.MeetingStatus,          
ic.InterviewStatus, ic.InterviewerComment, im.ClientHrContact, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency,          
im.staffingAgency, ice.InterviewStatus as InterviewStatusTwo, ice.MeetingStatus as MeetingStatusTwo,          
ice.InterviewerComment as InterviewerCommentTwo,          
tctv.ResumeLink, CandidateLatestStatus, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate,          
tctv.FirstScheduledOninterviewDate, tctv.completedDate        
, 1 as isQcDone, irf.IsDrive, ipr.NickName as InterviewerCodedName, im.Cancellationsource,CONCAT('https://portal.risebird.io/Interview/ExecutiveReport?interviewId=', im.id,'&irfId=',IRF.id,'&isClient=true') as ExecutiveReportLink,      
csc.Date as CurrentStatusDate,IRF.id as Irfintid,InterviewCount.NoOfInterviews,cscancel.CancelledBy,ibr.BillingRate  
from @TemporaryCandidateTableVariable tctv          
join InterviewMeetings im on im.id = tctv.InterviewId          
join IRF on IRF.IRFId=im.IRFId          
join InterviewComments ic on ic.InterviewId=im.id          
left join InterviewCommentsExtension ice on ice.Interviewid = im.id          
join UserList ul on ul.EmailId = im.candidateEmail          
left join UserList uli on uli.EmailId = im.interviewer          
left join InterviewerProfile ipr on ipr.UserId = uli.id          
left join PaymentSLADetails psd on psd.Interviewid = im.id          
left join Candidate ca on ca.IRFId=tctv.IRFId and ca.CandidateMasterId=tctv.CandidateMasterId     
left join InterviewerBillingRate ibr on ibr.IRFId=IRF.id and ibr.Userid=uli.id
outer apply(select Count(id) as NoOfInterviews from InterviewMeetings where IRFId=IRF.IRFId and candidateEmail=ul.EmailId) InterviewCount      
outer Apply (Select top 1 cso.StatusValue,DateAdd(Minute,330,cs.Date) as Date From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId order by cs.Date desc) csc        
outer Apply (Select top 1 PerformedBy as CancelledBy From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId and cs.Status=18 order by cs.Date desc) cscancel        
--where CandidateLatestStatus in ('SELECTED', 'REJECTED')          
where  irf.ClientId = 177 and CandidateLatestStatus like '%SELECTED%' or CandidateLatestStatus like '%REJECTED%'          
         
 union        
         
         
----pending          
         
select im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle, ic.MeetingStatus,          
ic.InterviewStatus, ic.InterviewerComment, im.ClientHrContact, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency,          
im.staffingAgency, ice.InterviewStatus as InterviewStatusTwo, ice.MeetingStatus as MeetingStatusTwo,          
ice.InterviewerComment as InterviewerCommentTwo,         
tctv.ResumeLink, CandidateLatestStatus, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate,          
tctv.FirstScheduledOninterviewDate,'' as completedDate, 1 as isQcDone, irf.IsDrive,        
ipr.NickName as InterviewerCodedName, im.Cancellationsource,'' as ExecutiveReportLink,      
csc.Date as CurrentStatusDate,IRF.id as Irfintid,InterviewCount.NoOfInterviews,cscancel.CancelledBy,ibr.BillingRate    
from @TemporaryCandidateTableVariable tctv          
join InterviewMeetings im on im.id = tctv.InterviewId          
join IRF on IRF.IRFId=im.IRFId          
join InterviewComments ic on ic.InterviewId=im.id          
left join InterviewCommentsExtension ice on ice.Interviewid = im.id          
join UserList ul on ul.EmailId = im.candidateEmail          
left join UserList uli on uli.EmailId = im.interviewer          
left join InterviewerProfile ipr on ipr.UserId = uli.id          
left join Candidate ca on ca.IRFId=tctv.IRFId and ca.CandidateMasterId=tctv.CandidateMasterId      
left join InterviewerBillingRate ibr on ibr.IRFId=IRF.id and ibr.Userid=uli.id
outer apply(select Count(id) as NoOfInterviews from InterviewMeetings where IRFId=IRF.IRFId and candidateEmail=ul.EmailId) InterviewCount      
outer Apply (Select top 1 cso.StatusValue,DateAdd(Minute,330,cs.Date) as Date From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId order by cs.Date desc) csc        
outer Apply (Select top 1 PerformedBy as CancelledBy From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId and cs.Status=18 order by cs.Date desc) cscancel        
where IRF.ClientId = 177 and CandidateLatestStatus in ('PENDING')          
         
 union        
----future scheduled          
select distinct im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle,ic.MeetingStatus,          
ic.InterviewStatus, ic.InterviewerComment, im.ClientHrContact, ic.VideoLink,im.confirmationStatus          
, ic.CommunicationalProficiency  , im.staffingAgency,         
ice.InterviewStatus as InterviewStatusTwo, ice.MeetingStatus as MeetingStatusTwo,          
ice.InterviewerComment as InterviewerCommentTwo          
, tctv.ResumeLink, CandidateLatestStatus,          
tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate, tctv.FirstScheduledOninterviewDate,'' as completedDate, 1 as isQcDone, irf.IsDrive,          
 ipr.NickName as InterviewerCodedName, im.Cancellationsource,'' as ExecutiveReportLink,      
 csc.Date as CurrentStatusDate,IRF.id as Irfintid,InterviewCount.NoOfInterviews,cscancel.CancelledBy,ibr.BillingRate    
from @TemporaryCandidateTableVariable tctv          
join InterviewMeetings im on im.id = tctv.InterviewId          
join InterviewComments ic on ic.InterviewId=im.id          
left join InterviewCommentsExtension ice on ice.Interviewid = im.id          
join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId = im.candidateEmail          
left join UserList uli on uli.EmailId = im.interviewer          
left join InterviewerProfile ipr on ipr.UserId = uli.id          
left join Candidate ca on ca.IRFId=tctv.IRFId and ca.CandidateMasterId=tctv.CandidateMasterId
left join InterviewerBillingRate ibr on ibr.IRFId=IRF.id and ibr.Userid=uli.id
outer apply(select Count(id) as NoOfInterviews from InterviewMeetings where IRFId=IRF.IRFId and candidateEmail=ul.EmailId) InterviewCount      
outer Apply (Select top 1 cso.StatusValue,DateAdd(Minute,330,cs.Date) as Date From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId order by cs.Date desc) csc        
outer Apply (Select top 1 PerformedBy as CancelledBy From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId and cs.Status=18 order by cs.Date desc) cscancel        
where IRF.ClientId = 177 and CandidateLatestStatus in ('FUTURESCHEDULE')          
        
union        
select im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle, ic.MeetingStatus,          
ic.InterviewStatus, ic.InterviewerComment, im.ClientHrContact, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency          
,im.staffingAgency, ice.InterviewStatus as InterviewStatusTwo, ice.MeetingStatus as MeetingStatusTwo,          
ice.InterviewerComment as InterviewerCommentTwo,        
tctv.ResumeLink, CandidateLatestStatus, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate,          
tctv.FirstScheduledOninterviewDate,'' as completedDate, 1 as isQcDone, irf.IsDrive, ipr.NickName as InterviewerCodedName,  im.Cancellationsource,'' as ExecutiveReportLink,      
csc.Date as CurrentStatusDate,IRF.id as Irfintid,InterviewCount.NoOfInterviews,cscancel.CancelledBy,ibr.BillingRate    
from @TemporaryCandidateTableVariable tctv          
join InterviewMeetings im on im.id = tctv.InterviewId          
join IRF on IRF.IRFId=im.IRFId          
join InterviewComments ic on ic.InterviewId=im.id         
left join InterviewCommentsExtension ice on ice.Interviewid = im.id          
join UserList ul on ul.EmailId = im.candidateEmail          
left join UserList uli on uli.EmailId = im.interviewer          
left join InterviewerProfile ipr on ipr.UserId = uli.id          
left join Candidate ca on ca.IRFId=tctv.IRFId and ca.CandidateMasterId=tctv.CandidateMasterId        
left join InterviewerBillingRate ibr on ibr.IRFId=IRF.id and ibr.Userid=uli.id
outer apply(select Count(id) as NoOfInterviews from InterviewMeetings where IRFId=IRF.IRFId and candidateEmail=ul.EmailId) InterviewCount      
outer Apply (Select top 1 cso.StatusValue,DateAdd(Minute,330,cs.Date) as Date From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId order by cs.Date desc) csc        
outer Apply (Select top 1 PerformedBy as CancelledBy From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId and cs.Status=18 order by cs.Date desc) cscancel        
where IRF.ClientId = 177 and CandidateLatestStatus in ('RESCHEDULE')        
             
                       
End 
GO


