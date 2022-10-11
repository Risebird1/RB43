  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchCallCatalogue'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchCallCatalogue]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
Create Procedure usp_FetchCallCatalogue      
@UserRole Varchar(15),      
@Emailid varchar(150)      
AS      
BEGIN      
if(@UserRole='Admin')      
BEGIN      
Select cd.*,crd.WhoCalled from (Select CTE.id,CTE.IRFId,CTE.CandidateEmail,CTE.CandidateName,CTE.JDTiltle,CTE.ClientName,Max(CTE.CalledOn) as CalledOn,Max(CTE.Callerid) as Callerid from (      
Select IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join IRF on crd.IRFId=IRF.id      
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=crd.CandidateEmail      
where interviewid=0 group by IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name      
union      
Select IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join InterviewMeetings im on im.id=crd.Interviewid      
join IRF on im.IRFId=IRF.IRFId      
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=im.candidateEmail      
where crd.interviewid!=0 group by IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name) as CTE      
group by CTE.id,CTE.IRFId,CTE.CandidateEmail,CTE.CandidateName,CTE.JDTiltle,CTE.ClientName) as Cd join CallRecordingDetails crd on cd.Callerid=crd.id      
order by CalledOn desc      
END 
else if(@UserRole='SeniorHR' or @UserRole='SupportAdmin')      
BEGIN      
Select cd.*,crd.WhoCalled from (Select CTE.id,CTE.IRFId,CTE.CandidateEmail,CTE.CandidateName,CTE.JDTiltle,CTE.ClientName,Max(CTE.CalledOn) as CalledOn,Max(CTE.Callerid) as Callerid from (      
Select IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join IRF on crd.IRFId=IRF.id 
join SeniorHR sh on sh.Clientid=IRF.ClientId
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=crd.CandidateEmail      
left join UserList ulsh on ulsh.id=sh.Userid
where ulsh.EmailId=@Emailid and interviewid=0 group by IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name      
union      
Select IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join InterviewMeetings im on im.id=crd.Interviewid      
join IRF on im.IRFId=IRF.IRFId      
join SeniorHR sh on sh.Clientid=IRF.ClientId
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=im.candidateEmail  
left join UserList ulsh on ulsh.id=sh.Userid
where ulsh.EmailId=@Emailid  and crd.interviewid!=0 group by IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name) as CTE      
group by CTE.id,CTE.IRFId,CTE.CandidateEmail,CTE.CandidateName,CTE.JDTiltle,CTE.ClientName) as Cd join CallRecordingDetails crd on cd.Callerid=crd.id      
order by CalledOn desc      
END 
ELSE      
BEGIN      
declare @hrId int    
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@Emailid and idProfile=2)        
Select cd.*,crd.WhoCalled from (Select CTE.id,CTE.IRFId,CTE.CandidateEmail,CTE.CandidateName,CTE.JDTiltle,CTE.ClientName,Max(CTE.CalledOn) as CalledOn,Max(CTE.Callerid) as Callerid from (      
Select IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join IRF on crd.IRFId=IRF.id      
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=crd.CandidateEmail      
join TransHR th on th.IRFId=irf.id       
where th.HRId=@hrId and interviewid=0 group by IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name      
union      
Select IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join InterviewMeetings im on im.id=crd.Interviewid      
join IRF on im.IRFId=IRF.IRFId      
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=im.candidateEmail      
join TransHR th on th.IRFId=irf.id       
where th.HRId=@hrId and crd.interviewid!=0 group by IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name      
union      
Select IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join IRF on crd.IRFId=IRF.id      
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=crd.CandidateEmail      
left join Userlist ulir on ulir.id=Irf.RisebirdHr      
where ulir.EmailId=@Emailid and interviewid=0 group by IRF.id,IRF.IRFId,crd.CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name      
union      
Select IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,IRF.JDTiltle,cl.Name as ClientName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid      
from CallRecordingDetails crd join InterviewMeetings im on im.id=crd.Interviewid      
join IRF on im.IRFId=IRF.IRFId      
join Client cl on cl.id=IRF.ClientId      
Left join userlist ul on ul.EmailId=im.candidateEmail      
left join Userlist ulir on ulir.id=Irf.RisebirdHr      
where ulir.EmailId=@Emailid and crd.interviewid!=0 group by IRF.id,IRF.IRFId,im.candidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')),IRF.JDTiltle,cl.Name) as CTE      
group by CTE.id,CTE.IRFId,CTE.CandidateEmail,CTE.CandidateName,CTE.JDTiltle,CTE.ClientName) as Cd join CallRecordingDetails crd on cd.Callerid=crd.id      
order by CalledOn desc  
END      
END      
    
  
  

GO