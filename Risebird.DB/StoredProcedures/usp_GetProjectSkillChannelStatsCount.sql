IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProjectSkillChannelStatsCount'))
BEGIN
    DROP PROCEDURE usp_GetProjectSkillChannelStatsCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetProjectSkillChannelStatsCount           
@startDate datetime,                
@endDate datetime,                        
@timeNow datetime,   
@emailid varchar(200)        
AS                
BEGIN       
DECLARE @tbl_Temp TABLE (                          
    IRFId INT,    
 ProjectId int,
 SkillSetsevaluate int
);     
  
Declare @Clientid int  
Select @Clientid=cu.ClientId From UserList ul join ClientUser cu on ul.id=cu.UserId where EmailId=@emailid and idProfile=4  
  
if(@Clientid>0)      
BEGIN      
INSERT INTO @tbl_Temp                        
select distinct pimd.IRFId,pr.id as ProjectId,pr.SkillSetsevaluate from CandidateStatus cs join IRF on IRF.id=cs.IRFId    
join ProjectIRFMapping pim on pim.IRFId=IRF.id    
join Project pr on pr.id=pim.ProjectId    
join ProjectIRFMapping pimd on pimd.ProjectId=pr.id    
Where IRF.ClientId=@Clientid and [Status] =12 and cast(cs.[Date] as date) <= cast(@endDate as date) and cast(cs.[Date] as date) >= cast(@startDate as date)    
END      
else      
begin      
INSERT INTO @tbl_Temp                          
select distinct pimd.IRFId,pr.id as ProjectId,pr.SkillSetsevaluate from CandidateStatus cs join IRF on IRF.id=cs.IRFId    
join ProjectIRFMapping pim on pim.IRFId=IRF.id    
join Project pr on pr.id=pim.ProjectId    
join ProjectIRFMapping pimd on pimd.ProjectId=pr.id    
Where [Status] =12 and cast(cs.[Date] as date) <= cast(@endDate as date) and cast(cs.[Date] as date) >= cast(@startDate as date)    
end        
  
        
DECLARE @TemporaryCandidateTableVariable TABLE (                          
 IRFId int,                    
 CandidateMasterId int,                    
 UserId int,               
 CandidateLatestStatus varchar(20),    
 ProjectId int    
);                       
                    
INSERT INTO @TemporaryCandidateTableVariable                          
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                      
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow),tt.ProjectId                    
from  Candidate ca                     
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                     
join CandidatesMaster cm on cm.id=ca.CandidateMasterId        
join @tbl_Temp tt on tt.IRFId=ca.IRFId   
where tt.SkillSetsevaluate=1
group by ca.IRFId, ca.CandidateMasterId,cm.UserId,tt.ProjectId                    

INSERT INTO @TemporaryCandidateTableVariable                          
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                      
dbo.fn_CandidateLatestStatusForIrfMultiStackStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow),tt.ProjectId                    
from  Candidate ca                     
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                     
join CandidatesMaster cm on cm.id=ca.CandidateMasterId        
join @tbl_Temp tt on tt.IRFId=ca.IRFId   
where tt.SkillSetsevaluate=2
group by ca.IRFId, ca.CandidateMasterId,cm.UserId,tt.ProjectId                    
                    
                    
  select distinct cl.[Name] as ClientCompName,pr.Name as ProjectName,pr.ProjectUniqueid,pr.SkillSetsevaluate,pr.id,cl.id as Clientid,    
 (select count(UserId) from @TemporaryCandidateTableVariable where ProjectId = pr.id) as CandidateCount,                      
 (select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus = 'FUTURESCHEDULE') as FutureScheduledMeetings,                     
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus in ('SELECTED', 'REJECTED','BOTHSELECTED','BOTHREJECTED','SELECTEDNREJECTED','REJECTEDNSELECTED'))  as CompletedInterviews,                    
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus in('SELECTED','BOTHSELECTED')) as SelectedMeetings,                    
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus in('REJECTED','BOTHREJECTED')) as RejectedMeetings, 
 (Select count(UserId) from @TemporaryCandidateTableVariable                      
where ProjectId = pr.id and CandidateLatestStatus = 'SELECTEDNREJECTED') as SelectedRejectMeetings,    
(Select count(UserId) from @TemporaryCandidateTableVariable                      
where ProjectId = pr.id and CandidateLatestStatus = 'REJECTEDNSELECTED') as RejectSelectedMeetings,    
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus = 'NOTSCHEDULED') as NotScheduledCount,                     
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus = 'RESCHEDULE') as ReScheduledCount,                    
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus = 'PENDING') as PendingCount,      
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ProjectId = pr.id and CandidateLatestStatus = 'COLDCANDIDATE') as ColdCandidateCount    
from @tbl_Temp tmp join Project pr on pr.id=tmp.ProjectId    
join Client cl on cl.id = pr.ClientId                      
    
END               
GO