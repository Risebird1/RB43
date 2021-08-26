
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
select distinct IRFId from CandidateStatus where [Status] =12 and [Date]<=@endDate and [Date] > @startDate 



DECLARE @TemporaryCandidateTableVariable AS tempCandidate_TableType  
INSERT INTO @TemporaryCandidateTableVariable    
select ca.id from  Candidate ca join CandidateStatus cs on (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId) where cs.[Status] =12 and [Date]<=@endDate and [Date] >= @startDate 


	select irf.id, irf.IRFId, irf.JDTiltle, cl.[Name] as ClientCompName, irf.JobCountry, IsNULL(ulb.EmailId, '') as BillingOwnerEmailId, ul.EmailId as RisebirdHrEmail, 
	(select count(ca.id) from Candidate ca join @TemporaryCandidateTableVariable tmpC on tmpC.id =ca.id where IRFId = irf.id) as CandidateCount,
	(select count(tctv.id) from @TemporaryCandidateTableVariable tctv 
	join Candidate ca on ca.id = tctv.id
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF as irfSI on irfSI.id = ca.IRFId
	where dbo.fn_HasCandidateScheduledInterview(ul.EmailId, irfSI.IRFId, @startDate)=1 and irfSI.id=IRF.id) as ScheduledMeetings,

	(select count(tctv.id) from @TemporaryCandidateTableVariable tctv 
	join Candidate ca on ca.id = tctv.id
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF as irfFI on irfFI.id = ca.IRFId
	where dbo.fn_HasCandidateScheduledFutureInterview(ul.EmailId, irfFI.IRFId, @timeNow)=1 and irfFI.id=IRF.id) as FutureScheduledMeetings,

	(select count(tctv.id) from @TemporaryCandidateTableVariable tctv 
	join Candidate ca on ca.id = tctv.id
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF as irfCI on irfCI.id = ca.IRFId
	where dbo.fn_HasCandidateCompletedInterview(ul.EmailId, irfCI.IRFId, @startDate)=1 and irfCI.id=IRF.id)  as CompletedInterviews,
	
	(select count(tctv.id) from @TemporaryCandidateTableVariable tctv 
	join Candidate ca on ca.id = tctv.id
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF as irfSI on irfSI.id = ca.IRFId
	where dbo.fn_IsCandidateSelected(ul.EmailId, irfSI.IRFId, @startDate)=1 and irfSI.id=IRF.id) as SelectedMeetings,

	(select count(tctv.id) from @TemporaryCandidateTableVariable tctv 
	join Candidate ca on ca.id = tctv.id
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF as irfRI on irfRI.id = ca.IRFId
	where dbo.fn_IsCandidateRejected(ul.EmailId, irfRI.IRFId, @startDate)=1 and irfRI.id=IRF.id) as RejectedMeetings,

	(select count(tctv.id) from @TemporaryCandidateTableVariable tctv 
	join Candidate ca on ca.id = tctv.id
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF as irfNS on irfNS.id = ca.IRFId
	where dbo.fn_HasCandidateNotScheduled(ul.EmailId, irfNS.IRFId, @startDate)=1 and irfNS.id=IRF.id) as NotScheduledCount,

	(select count(tctv.id) from @TemporaryCandidateTableVariable tctv 
	join Candidate ca on ca.id = tctv.id
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF as irfRS on irfRS.id = ca.IRFId
	where dbo.fn_HasCandidateReScheduled(ul.EmailId, irfRS.IRFId, @startDate)=1 and irfRS.id=IRF.id) as ReScheduledCount
	
	from IRF
	join Client cl on cl.id = irf.ClientId
	join UserList ul on ul.id = irf.RisebirdHr
	left join UserList ulb on ulb.id = irf.BillingOwner
    join @tbl_Temp tmp on tmp.IRFId =IRF.id

End    
  
GO

