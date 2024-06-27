IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetNotSecheduleSeniorHR'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotSecheduleSeniorHR
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetNotSecheduleSeniorHR]     
@Emailid varchar(150)
As
Begin
;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber, irfIntId, ChannelOwner) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber, irf.id as irfIntId, ulir.EmailId as ChannelOwner  From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId                 
join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId join SeniorHr sh on sh.Clientid=IRF.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join Userlist ulir on ulir.id=irf.RisebirdHr where ulir.Emailid=@Emailid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0                     
Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber, irf.id as irfIntId, ulir.EmailId as ChannelOwner from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail join SeniorHr sh on sh.Clientid=IRF.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join Userlist ulir on ulir.id=irf.RisebirdHr where ulir.Emailid=@Emailid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0)                        
Select Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,Max(dtCreatedOn) as dtCreatedOn,CalendlyLink,ClientName,JobCountry, CandidateMasterId, Cd.ChannelOwner,Cd.StatusValue,Cd.CandidateId  from           
(Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,    
c.ClientHRAttemptContact as ClientHrContact,DateAdd(minute,330,cs.Date) as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName,    
IRF.JobCountry, cm.id as CandidateMasterId, ulir.EmailId as ChannelOwner,CandidateStatus.StatusValue,c.id as CandidateId     
from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id                       
left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12                      
join Client cl on cl.id=IRF.ClientId 
left join Userlist ulir on ulir.id=irf.RisebirdHr
left join ColdCandidateInfos cci on c.id=cci.FKCandidateId
outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.irfIntId and CandidateId = CandidateMasterId order by cs.Date desc) as CandidateStatus
Where ulir.Emailid=@Emailid and cs.Date is not Null AND ISNULL(cci.IsColdCandidate,0) !=1                    
group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,DateAdd(minute,330,cs.Date),    
IRF.CalendlyLink,cl.Name,IRF.JobCountry, cm.id,ulir.EmailId,CandidateStatus.StatusValue,c.id having Count(*)=1) as Cd               
group by Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,    
CalendlyLink,ClientName,JobCountry, Cd.CandidateMasterId, Cd.ChannelOwner,Cd.StatusValue,Cd.CandidateId               
order by dtCreatedOn desc   
End 
GO