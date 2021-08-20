
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientReScheduledMeetings'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetClientReScheduledMeetings]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
   
                  
Create Procedure usp_GetClientReScheduledMeetings        
@Clientid int,              
@Emailid varchar(150)              
AS                  
Begin        
if(@Clientid>0)          
Begin        

declare @clientRole int
set @clientRole =(select top 1 cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId =@Emailid)

if(@clientRole=4)--staffing Agency
begin
	Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName from (Select distinct imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn          
	from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
	ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
	from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
	join IRF on im.IRFId=IRF.IRFId         
	left join UserList ul on ul.EmailId=im.candidateEmail
	join CandidatesMaster cm on cm.UserId=ul.id
	join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id 
	join TransClientUser tcu on tcu.IRFId=IRF.id              
	left join Userlist tcul on tcul.id=tcu.ClientUserId              
	where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and c.ProfileSource=@Emailid 
	union      
	Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
	ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
	from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id         
	join IRF on im.IRFId=IRF.IRFId         
	left join UserList ul on ul.EmailId=im.candidateEmail
	join CandidatesMaster cm on cm.UserId=ul.id
	join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id
	join TransClientUser tcu on tcu.IRFId=IRF.id              
	left join Userlist tcul on tcul.id=tcu.ClientUserId              
	where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and c.ProfileSource=@Emailid) as cd       
	where MeetingStatus=1 or MeetingStatus=3 or confirmationStatus=2
	group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber)as xy         
	join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId      
	join InterviewComments ic on im.id=ic.interviewid  
	join IRF on im.IRFId=IRF.IRFId  
	join Client cl on cl.id=IRF.ClientId
	order by dtCreatedOn desc 
end
else
begin
	Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName from (Select distinct imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn          
	from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
	ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
	from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
	join IRF on im.IRFId=IRF.IRFId         
	left join UserList ul on ul.EmailId=im.candidateEmail        
	join TransClientUser tcu on tcu.IRFId=IRF.id              
	left join Userlist tcul on tcul.id=tcu.ClientUserId              
	where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0        
	union      
	Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
	ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
	from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id         
	join IRF on im.IRFId=IRF.IRFId         
	left join UserList ul on ul.EmailId=im.candidateEmail        
	join TransClientUser tcu on tcu.IRFId=IRF.id              
	left join Userlist tcul on tcul.id=tcu.ClientUserId              
	where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0) as cd       
	where MeetingStatus=1 or MeetingStatus=3 or confirmationStatus=2      
	group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber)as xy         
	join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId      
	join InterviewComments ic on im.id=ic.interviewid  
	join IRF on im.IRFId=IRF.IRFId  
	join Client cl on cl.id=IRF.ClientId
	order by dtCreatedOn desc 
end
      
        
      
End        
Else        
Begin        
Declare @Role int          
Select @Role=idProfile from Userlist where Emailid=@Emailid          
if(@Role=1)          
Begin        
      
Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName from (Select imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn          
from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId         
left join UserList ul on ul.EmailId=im.candidateEmail        
where IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0        
union      
Select distinct IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId         
left join UserList ul on ul.EmailId=im.candidateEmail        
where IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0) as cd       
where MeetingStatus=1 or MeetingStatus=3 or confirmationStatus=2      
group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber)as xy         
join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail      
join InterviewComments ic on im.id=ic.interviewid    
join IRF on im.IRFId=IRF.IRFId  
join Client cl on cl.id=IRF.ClientId
order by dtCreatedOn desc       
      
End        
Else     
Begin        
declare @hrId int            
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@Emailid and idProfile=2)            
      
Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName from (Select imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn          
from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId         
left join UserList ul on ul.EmailId=im.candidateEmail        
join TransHR th on th.IRFId=irf.id         
where th.HRId=@hrId and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0        
union        
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId         
left join UserList ul on ul.EmailId=im.candidateEmail        
left join Userlist ulir on ulir.id=irf.RisebirdHr        
where ulir.EmailId=@Emailid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0        
union       
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId         
left join UserList ul on ul.EmailId=im.candidateEmail        
join TransHR th on th.IRFId=irf.id         
where th.HRId=@hrId and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0        
union        
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.resumeLink,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId         
left join UserList ul on ul.EmailId=im.candidateEmail        
left join Userlist ulir on ulir.id=irf.RisebirdHr        
where ulir.EmailId=@Emailid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0) as cd       
where MeetingStatus=1 or MeetingStatus=3 or confirmationStatus=2      
group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber)as xy         
join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId      
join InterviewComments ic on im.id=ic.interviewid   
join IRF on im.IRFId=IRF.IRFId  
join Client cl on cl.id=IRF.ClientId
order by dtCreatedOn desc           
      
End        
End        
End        
    
      
  

GO