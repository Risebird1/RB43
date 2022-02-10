
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFCandidateStats'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCandidateStats
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
      
CREATE PROCEDURE [dbo].usp_GetIRFCandidateStats  
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
	UserId int,
	CandidateLatestStatus varchar(20)
);   

INSERT INTO @TemporaryCandidateTableVariable      
select ca.IRFId, ca.CandidateMasterId, cm.UserId,  
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow)
from  Candidate ca 
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId) 
join CandidatesMaster cm on cm.id=ca.CandidateMasterId  
where cs.[Status] =12 and cast([Date] as date) <=cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)
group by ca.IRFId, ca.CandidateMasterId,cm.UserId


  select irf.id, irf.IRFId, irf.JDTiltle, cl.[Name] as ClientCompName, irf.JobCountry, IsNULL(ulb.EmailId, '') as BillingOwnerEmailId, ul.EmailId as RisebirdHrEmail,   
 
 (select count(UserId) from @TemporaryCandidateTableVariable where IRFId = irf.id) as CandidateCount,  
  
 (select count(UserId) from @TemporaryCandidateTableVariable    
 where IRFId=IRF.id and CandidateLatestStatus = 'FUTURESCHEDULE') as FutureScheduledMeetings, 
 
 (Select count(UserId) from @TemporaryCandidateTableVariable    
 where IRFId=IRF.id and CandidateLatestStatus in ('SELECTED', 'REJECTED'))  as CompletedInterviews,
 
 (Select count(UserId) from @TemporaryCandidateTableVariable    
 where IRFId=IRF.id and CandidateLatestStatus = 'SELECTED') as SelectedMeetings,
 
 (Select count(UserId) from @TemporaryCandidateTableVariable    
 where IRFId=IRF.id and CandidateLatestStatus = 'REJECTED') as RejectedMeetings, 

 (Select count(UserId) from @TemporaryCandidateTableVariable    
 where IRFId=IRF.id and CandidateLatestStatus = 'NOTSCHEDULED') as NotScheduledCount, 

 (Select count(UserId) from @TemporaryCandidateTableVariable    
 where IRFId=IRF.id and CandidateLatestStatus = 'RESCHEDULE') as ReScheduledCount,
 
 (Select count(UserId) from @TemporaryCandidateTableVariable    
 where IRFId=IRF.id and CandidateLatestStatus = 'PENDING') as PendingCount 
 
 from IRF  
 join @tbl_Temp tmp on tmp.IRFId =IRF.id  
 join Client cl on cl.id = irf.ClientId  
 join UserList ul on ul.id = irf.RisebirdHr  
 left join UserList ulb on ulb.id = irf.BillingOwner  
  
End      
        
  
GO

