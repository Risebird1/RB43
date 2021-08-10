IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientNotScheduledMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientNotScheduledMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetClientNotScheduledMeetings             
@Clientid int,          
@Emailid varchar(150)          
AS              
BEGIN              
if(@Clientid>0)      
begin      
;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId       
join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where irf.ClientId=@Clientid  and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0           
Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where irf.ClientId=@Clientid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0)              
Select Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,Max(dtCreatedOn) as dtCreatedOn,CalendlyLink,ClientName from
(Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id
left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id            
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12            
join TransClientUser tcu on tcu.IRFId=IRF.id          
left join Userlist tcul on tcul.id=tcu.ClientUserId 
join Client cl on cl.id=IRF.ClientId
Where tcul.EmailId=@Emailid and cs.Date is not Null          
group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date,IRF.CalendlyLink,cl.Name having Count(*)=1) as Cd              
group by Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,CalendlyLink,ClientName    
order by dtCreatedOn desc           
       
End      
Else      
Begin      
Declare @Role int      
Select @Role=idProfile from Userlist where Emailid=@Emailid      
if(@Role=1)      
Begin      
;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId       
join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0           
Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0)              
Select Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,Max(dtCreatedOn) as dtCreatedOn,CalendlyLink,ClientName from 
(Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id             
left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id            
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12            
join Client cl on cl.id=IRF.ClientId
Where cs.Date is not Null          
group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date,IRF.CalendlyLink,cl.Name having Count(*)=1) as Cd     
group by Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,CalendlyLink,ClientName    
order by dtCreatedOn desc           
     END      
Else      
Begin      
declare @hrId int        
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@Emailid and idProfile=2)        
;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as ((Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId       
join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId join TransHR th on th.IRFId=irf.id where th.HRId=@hrId and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0      
union Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId       
join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId join Userlist uli on uli.id=irf.RisebirdHr where uli.Emailid=@Emailid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0)      
Union All (Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail join TransHR th on th.IRFId=irf.id where th.HRId=@hrId and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0      
union Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail
join Userlist uli on uli.id=irf.RisebirdHr where uli.Emailid=@Emailid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0))              
Select Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,Max(dtCreatedOn) as dtCreatedOn,CalendlyLink,ClientName from
(Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id
left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id            
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12            
join Client cl on cl.id=IRF.ClientId
Where cs.Date is not Null          
group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date,IRF.CalendlyLink,cl.Name having Count(*)=1) as Cd              
group by Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,CalendlyLink,ClientName    
order by dtCreatedOn desc           
      
END      
END      
END           
  
GO


