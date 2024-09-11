  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetPartnerDetailsStatsCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPartnerDetailsStatsCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetPartnerDetailsStatsCount  
@startDate datetime,                
@endDate datetime,                        
@timeNow datetime,   
@emailid varchar(200)        
AS                
BEGIN       
Declare @Clientid int
Select @Clientid=cu.ClientId From UserList ul join ClientUser cu on ul.id=cu.UserId where EmailId=@emailid and idProfile=4  
declare @notinclude varchar(50)
set @notinclude='@'+SUBSTRING(@emailid,LEN(@emailid)-CHARINDEX('@',REVERSE(@emailid))+ 2, LEN(@emailid))

DECLARE @TemporaryCandidateTableVariable TABLE (                          
 IRFId int,                    
 CandidateMasterId int,                    
 UserId int,               
 CandidateLatestStatus varchar(20),
 ClientHRAttemptContact varchar(200)
);                       

INSERT INTO @TemporaryCandidateTableVariable                        
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                    
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow),ca.ClientHRAttemptContact
from  Candidate ca                   
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                   
join CandidatesMaster cm on cm.id=ca.CandidateMasterId  
join IRF on IRF.id=ca.IRFId
Where IRF.ClientId=@Clientid  and cs.[Status] =12 and cast(cs.[Date] as date) <= cast(@endDate as date) and cast(cs.[Date] as date) >= cast(@startDate as date)
group by ca.IRFId, ca.CandidateMasterId,cm.UserId,ca.ClientHRAttemptContact
 
 select distinct cl.[Name] as ClientCompName,cl.id as Clientid,ul.Emailid as ClientHRAttemptContact,ul.id,    
 (select count(UserId) from @TemporaryCandidateTableVariable where ClientHRAttemptContact = ul.Emailid) as CandidateCount,                      
 (select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus = 'FUTURESCHEDULE') as FutureScheduledMeetings,                     
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus in ('SELECTED', 'REJECTED'))  as CompletedInterviews,                    
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus = 'BOTHSELECTED') as SelectedMeetings,                    
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus = 'BOTHREJECTED') as RejectedMeetings,                     
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus = 'NOTSCHEDULED') as NotScheduledCount,                     
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus = 'RESCHEDULE') as ReScheduledCount,                    
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus = 'PENDING') as PendingCount,      
 (Select count(UserId) from @TemporaryCandidateTableVariable                        
 where ClientHRAttemptContact = ul.Emailid and CandidateLatestStatus = 'COLDCANDIDATE') as ColdCandidateCount   
 From ClientUser cu join UserList ul on ul.id=cu.Userid join Client cl on cl.id = cu.ClientId
 where cu.Clientid=@Clientid and ul.Emailid not LIKE '%' + @notinclude + '%'
END 
GO