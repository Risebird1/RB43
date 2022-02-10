
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRFDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
      
CREATE PROCEDURE [dbo].[usp_GetAllIRFDetails]  
@startDate datetime,  
@endDate datetime,  
@timeNow datetime  
  
AS   
Begin  
  

DECLARE @tbl_Temp TABLE (      
    IRFId INT        
);      
INSERT INTO @tbl_Temp      
select distinct IRFId from CandidateStatus where [Status] =12 and cast([Date] as date) <= cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)   

DECLARE @TemporaryCandidateTableVariable TABLE (      
	IRFId int,
	CandidateMasterId int,
	UserId int
);   

INSERT INTO @TemporaryCandidateTableVariable      
select ca.IRFId, ca.CandidateMasterId,cm.UserId from  Candidate ca 
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId) 
join CandidatesMaster cm on cm.id=ca.CandidateMasterId  
where cs.[Status] =12 and cast([Date] as date) <=cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)
group by ca.IRFId, ca.CandidateMasterId,cm.UserId  

 select irf.id, irf.IRFId, irf.JDTiltle, cl.[Name] as ClientCompName, irf.JobCountry, IsNULL(ulb.EmailId, '') as BillingOwnerEmailId, ul.EmailId as RisebirdHrEmail,   
 (select count(UserId) from @TemporaryCandidateTableVariable where IRFId = irf.id) as CandidateCount,  
 
 --(select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 --join UserList ul on ul.id = tctv.UserId  
 --join IRF as irfSI on irfSI.id = tctv.IRFId  
 --join interviewmeetings im on im.candidateEmail=ul.Emailid and im.IRFId=irfSI.IRFId
 --join InterviewComments ic on im.id=ic.Interviewid
 --where tctv.IRFId=IRF.id and ((im.confirmationStatus=1 and ic.MeetingStatus=4) or (im.confirmationStatus=1 and (ic.MeetingStatus=1 or ic.MeetingStatus=3 or ic.MeetingStatus=5)) or confirmationStatus=2)) as ScheduledMeetings,
 
  (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateEverScheduledTillNow(tctv.UserId, tctv.IRFId, cast(@startDate as date), @timeNow) = 1) as ScheduledMeetings,

 (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateScheduledInfuture(tctv.UserId, tctv.IRFId, @timeNow) = 1) as FutureScheduledMeetings, 
 
  (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateInterviewCompleted(tctv.UserId, tctv.IRFId, cast(@startDate as date)) = 1)  as CompletedInterviews,
 
 (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateSelected(tctv.UserId, tctv.IRFId, cast(@startDate as date)) = 1) as SelectedMeetings,
 
 (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateRejected(tctv.UserId, tctv.IRFId, cast(@startDate as date)) = 1) as RejectedMeetings, 

 (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateNeverScheduled(tctv.UserId, tctv.IRFId, cast(@startDate as date)) = 1) as NotScheduledCount, 

 (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateEligibleForRescheduling(tctv.UserId, tctv.IRFId, @startDate) = 1) as ReScheduledCount,
 
 (select count(tctv.UserId) from @TemporaryCandidateTableVariable tctv   
 where tctv.IRFId=IRF.id and dbo.fn_IsCandidateReportPending(tctv.UserId, tctv.IRFId, cast(@startDate as date), @timeNow) = 1) as PendingCount 
 
 from IRF  
 join @tbl_Temp tmp on tmp.IRFId =IRF.id  
 join Client cl on cl.id = irf.ClientId  
 join UserList ul on ul.id = irf.RisebirdHr  
 left join UserList ulb on ulb.id = irf.BillingOwner  
  
End      
        
  
GO

