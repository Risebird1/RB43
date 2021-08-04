
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
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,candidateName,IRF.JDTiltle,
ul.MobileNumber,im.resumeLink,im.HRCoordinatorEmailId as ClientHrContact,im.interviewDate as dtCreatedOn,ic.MeetingStatus 
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id 
join IRF on im.IRFId=IRF.IRFId 
left join UserList ul on ul.EmailId=im.candidateEmail
join TransClientUser tcu on tcu.IRFId=IRF.id      
left join Userlist tcul on tcul.id=tcu.ClientUserId      
where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and (ic.MeetingStatus=1 or ic.MeetingStatus=3) and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0
order by im.interviewDate
End
Else
Begin
Declare @Role int  
Select @Role=idProfile from Userlist where Emailid=@Emailid  
if(@Role=1)  
Begin
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,candidateName,IRF.JDTiltle,
ul.MobileNumber,im.resumeLink,im.HRCoordinatorEmailId as ClientHrContact,im.interviewDate as dtCreatedOn,ic.MeetingStatus 
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id 
join IRF on im.IRFId=IRF.IRFId 
left join UserList ul on ul.EmailId=im.candidateEmail
where (ic.MeetingStatus=1 or ic.MeetingStatus=3) and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0
order by im.interviewDate
End
Else 
Begin
declare @hrId int    
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@Emailid and idProfile=2)    
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,candidateName,IRF.JDTiltle,
ul.MobileNumber,im.resumeLink,im.HRCoordinatorEmailId as ClientHrContact,im.interviewDate as dtCreatedOn,ic.MeetingStatus 
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id 
join IRF on im.IRFId=IRF.IRFId 
left join UserList ul on ul.EmailId=im.candidateEmail
join TransHR th on th.IRFId=irf.id 
where th.HRId=@hrId and (ic.MeetingStatus=1 or ic.MeetingStatus=3) and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0
union
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,candidateName,IRF.JDTiltle,
ul.MobileNumber,im.resumeLink,im.HRCoordinatorEmailId as ClientHrContact,im.interviewDate as dtCreatedOn,ic.MeetingStatus 
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id 
join IRF on im.IRFId=IRF.IRFId 
left join UserList ul on ul.EmailId=im.candidateEmail
left join Userlist ulir on ulir.id=irf.RisebirdHr
where ulir.EmailId=@Emailid and (ic.MeetingStatus=1 or ic.MeetingStatus=3) and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0
order by im.interviewDate
End
End
End


