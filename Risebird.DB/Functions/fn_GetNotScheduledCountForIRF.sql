
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_GetNotScheduledCountForIRF]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_GetNotScheduledCountForIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_GetNotScheduledCountForIRF(@irfId int, @TemporaryCandidateTableVariable dbo.tempCandidate_TableType READONLY)
Returns int
as
Begin

declare @totalCount int
set @totalCount=0
--set @totalCount = 
;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) 
as 
(Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,
IRF.JDTiltle,ul.MobileNumber 
From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId
join @TemporaryCandidateTableVariable tempC on c.id =tempC.id
join UserList ul on ul.id=cm.UserId 
join irf on irf.id=c.IRFId 
where irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and irf.id = @irfId          
Union All 
Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,
IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im 
join irf on irf.IRFId=im.IRFId 
join Userlist ul on ul.EmailId=im.candidateEmail 
where irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and irf.id = @irfId)              
select @totalCount = count(a.imid) 
from (Select Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,
Cd.ClientHrContact,Max(dtCreatedOn) as dtCreatedOn,CalendlyLink,ClientName from 
(Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,
CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,
cs.Date as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName 
from CTE 
join IRF on CTE.IRFID=IRF.IRFId 
left join Userlist ul on ul.EmailId=CTE.Emailid 
left join CandidatesMaster cm on cm.UserId=ul.id             
left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id            
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12     
join @TemporaryCandidateTableVariable tempC on c.id =tempC.id
join Client cl on cl.id=IRF.ClientId
Where cs.Date is not Null and irf.id = @irfId       
group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date,IRF.CalendlyLink,cl.Name having Count(*)=1) as Cd     
group by Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,CalendlyLink,ClientName) a 

 return @totalCount

End