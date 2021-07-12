IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientNotScheduledMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientNotScheduledMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_GetClientNotScheduledMeetings  
@Clientid int  
AS  
BEGIN  
;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where irf.ClientId=@Clientid  
Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where irf.ClientId=@Clientid)  
Select IRF.id as imid,CTE.Emailid as candidateEmail,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink from CTE join IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id group by IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink having Count(*)=1  
END  
GO


