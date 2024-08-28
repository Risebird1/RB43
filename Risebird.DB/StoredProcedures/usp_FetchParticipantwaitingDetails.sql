IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchParticipantwaitingDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchParticipantwaitingDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchParticipantwaitingDetails  
@Emailid varchar(150) 
AS  
BEGIN  
Declare @Role varchar(150)              
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid   
if(@Role='SeniorHR' or @Role = 'HR')--or @Role='SupportAdmin'              
BEGIN
Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.*, IRF.Id as irfIntId,imj.Joinedon,pc.PerformedOn   
from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr   
left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer   
left join SeniorHR sr on sr.Clientid=IRF.ClientId left join UserList ulij on ulij.id=sr.Userid
Outer Apply(Select Max(Joinedon) as Joinedon From InterviewMeetingsJoinTime imjt where im.id=imjt.interviewid) imj  
Outer Apply(Select Max(PerformedOn) as PerformedOn From PageClickDetails pd where pd.Interviewid=im.id and ButtonAction='JoinMeeting') pc  
Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined   
where ulij.EmailId=@Emailid and im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1
and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))  

End
else
Begin
Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.*, IRF.Id as irfIntId,imj.Joinedon,pc.PerformedOn   
from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr   
left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer   
Outer Apply(Select Max(Joinedon) as Joinedon From InterviewMeetingsJoinTime imjt where im.id=imjt.interviewid) imj  
Outer Apply(Select Max(PerformedOn) as PerformedOn From PageClickDetails pd where pd.Interviewid=im.id and ButtonAction='JoinMeeting') pc  
Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined   
where im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))  
End
END   
Go