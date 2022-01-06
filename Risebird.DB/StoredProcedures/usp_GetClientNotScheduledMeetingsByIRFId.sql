  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientNotScheduledMeetingsByIRFId'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetClientNotScheduledMeetingsByIRFId]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetClientNotScheduledMeetingsByIRFId         
@IRFId varchar(50),
@emailId varchar(50)=null
AS          
BEGIN          
declare @roleId int

if(@emailId is not null)
begin
	declare @profile varchar(150)
	set @profile = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile where ul.EmailId=@emailId)
	if(@profile =  'Client')
	begin
		set @roleId = (select cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId = @emailId)
	end
end

if(@roleId =1)
begin
	;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.
	CandidateMasterId   
	join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where IRF.IRFId=@IRFId   
	Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where IRF.IRFId=@IRFId)          
	Select Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,Max(dtCreatedOn) as dtCreatedOn from(Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id         
	left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id        
	left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12        
	Where cs.Date is not Null  and c.ClientHRAttemptContact = @emailId
	group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date having Count(*)=1) as Cd          
	group by Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact
	order by dtCreatedOn desc
end
else
begin
	;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.
	CandidateMasterId   
	join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where IRF.IRFId=@IRFId   
	Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where IRF.IRFId=@IRFId)          
	Select Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,Max(dtCreatedOn) as dtCreatedOn from(Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id         
	left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id        
	left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12        
	Where cs.Date is not Null  
	group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date having Count(*)=1) as Cd          
	group by Cd.imid,Cd.candidateEmail,Cd.IRFId,Cd.candidateName,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact
	order by dtCreatedOn desc 
end



   	            

END  
GO
  

