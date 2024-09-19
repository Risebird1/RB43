IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUserDetailsStatsCount'))
BEGIN
    DROP PROCEDURE usp_GetUserDetailsStatsCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetUserDetailsStatsCount    
@startDate datetime,                  
@endDate datetime,                          
@timeNow datetime,     
@emailid varchar(200)          
AS                  
BEGIN         
Declare @Clientid int  
Declare @RoleId int  
Select @Clientid=cu.ClientId,@RoleId=RoleId From UserList ul join ClientUser cu on ul.id=cu.UserId where EmailId=@emailid and idProfile=4    
Select * from ClientRole  
  
DECLARE @tbl_Temp TABLE (                        
    IRFId INT                          
);     
  
INSERT INTO @tbl_Temp                        
select distinct cs.IRFId from CandidateStatus cs join IRF on IRF.id=cs.IRFId join UserList ul on ul.id = irf.RisebirdHr       
join TransClientUser tc on tc.IRFId=IRF.id join UserList uli on uli.id=tc.ClientUserId      
Where IRF.ClientId=@Clientid and uli.EmailId=@emailid and cs.[Status] =12 and cast(cs.[Date] as date) <= cast(@endDate as date) and cast(cs.[Date] as date) >= cast(@startDate as date)  
  
DECLARE @TemporaryCandidateTableVariable TABLE (                            
 IRFId int,                  
 CandidateMasterId int,                  
 UserId int,             
 CandidateLatestStatus varchar(20)    
);        
  
if(@RoleId=1)  
begin  
INSERT INTO @TemporaryCandidateTableVariable                          
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                      
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow)  
from  Candidate ca                     
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                     
join CandidatesMaster cm on cm.id=ca.CandidateMasterId    
join IRF on IRF.id=ca.IRFId  
join TransClientUser tcu on tcu.IRFId=IRF.id                              
left join Userlist tcul on tcul.id=tcu.ClientUserId    
Where IRF.ClientId=@Clientid and tcul.EmailId=@Emailid and ca.ClientHRAttemptContact=@emailid and cs.[Status] =12 and cast(cs.[Date] as date) <= cast(@endDate as date) and cast(cs.[Date] as date) >= cast(@startDate as date)  
group by ca.IRFId, ca.CandidateMasterId,cm.UserId  
end  
else if(@RoleId=4)  
begin  
INSERT INTO @TemporaryCandidateTableVariable                          
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                      
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow)  
from  Candidate ca                     
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                     
join CandidatesMaster cm on cm.id=ca.CandidateMasterId    
join IRF on IRF.id=ca.IRFId  
join TransClientUser tcu on tcu.IRFId=IRF.id                              
left join Userlist tcul on tcul.id=tcu.ClientUserId    
Where tcul.EmailId=@Emailid and ca.ProfileSource=@Emailid and IRF.ClientId=@Clientid and cs.[Status] =12 and cast(cs.[Date] as date) <= cast(@endDate as date) and cast(cs.[Date] as date) >= cast(@startDate as date)  
group by ca.IRFId, ca.CandidateMasterId,cm.UserId  
end  
   
 select irf.id, irf.IRFId, Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle, cl.[Name] as ClientCompName, irf.JobCountry, IsNULL(ulb.EmailId, '') as BillingOwnerEmailId, ul.EmailId as RisebirdHrEmail,    
  (CASE WHEN IRF.IsPaused=1 Then'Paused' WHEN IRF.IsDeleted=1 THEN 'Archived' WHEN IRF.EndDate>=GETDATE() THEN 'Active' ELSE 'Archived' END) AS IRFStatus,    
 (select count(UserId) from @TemporaryCandidateTableVariable where IRFId = irf.id) as CandidateCount,                    
 (select count(UserId) from @TemporaryCandidateTableVariable                      
 where IRFId=IRF.id and CandidateLatestStatus = 'FUTURESCHEDULE') as FutureScheduledMeetings,                   
 (Select count(UserId) from @TemporaryCandidateTableVariable                      
 where IRFId=IRF.id and CandidateLatestStatus in ('SELECTED', 'REJECTED'))  as CompletedInterviews,                  
 (Select count(UserId) from @TemporaryCandidateTableVariable                      
 where IRFId=IRF.id and CandidateLatestStatus = 'NOTSCHEDULED') as NotScheduledCount,                   
 (Select count(UserId) from @TemporaryCandidateTableVariable                      
 where IRFId=IRF.id and CandidateLatestStatus = 'RESCHEDULE') as ReScheduledCount,                  
 (Select count(UserId) from @TemporaryCandidateTableVariable                      
 where IRFId=IRF.id and CandidateLatestStatus = 'PENDING') as PendingCount,    
 (Select count(UserId) from @TemporaryCandidateTableVariable                      
 where IRFId=IRF.id and CandidateLatestStatus = 'COLDCANDIDATE') as ColdCandidateCount  
 from IRF                    
 join @tbl_Temp tmp on tmp.IRFId =IRF.id                    
 join Client cl on cl.id = irf.ClientId                    
 join UserList ul on ul.id = irf.RisebirdHr                    
 left join UserList ulb on ulb.id = irf.BillingOwner          
END 
GO