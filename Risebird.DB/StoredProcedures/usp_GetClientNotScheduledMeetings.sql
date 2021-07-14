IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientNotScheduledMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientNotScheduledMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetClientNotScheduledMeetings  
@Clientid int  
AS  
BEGIN  
;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where irf.ClientId=@Clientid  
Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where irf.ClientId=@Clientid)  
Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id 
left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12
group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date having Count(*)=1  
order by cs.Date desc

END  

GO


