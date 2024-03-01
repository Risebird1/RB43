IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewManagementDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewManagementDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO      
Create PROCEDURE [dbo].[usp_GetInterviewManagementDetails]    
@startdate datetime,          
@enddate datetime,
@Emailid varchar(150)
AS            
Begin    
declare   
@startDateParam datetime,  
@endDateParam datetime  
  
set @startDateParam =@startdate  
set @endDateParam =@enddate  
Declare @Role varchar(150)
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid
if(@Role='Client')
begin
Declare @Clientid int
Select @Clientid=cu.ClientId From UserList ul join ClientUser cu on cu.UserId=ul.id where ul.Emailid=@Emailid
Select Distinct im.id,IRF.id as irfIntId,im.interviewDate,imj.Joinedon,imjf.FirstJoinedon, ic.MeetingStatus,tzme.Joinedcount as TotalCount,im.IsMeetingNotificationSent,im.IsNotificationSentToEI,im.IsRescheduleNotificationSent,im.IsEmergency,im.EIAssigned,imc.PerformedOn,imcf.FirstPerformedOn,im.confirmationStatus,ip.NickName as interviewer
,im.candidateEmail,cl.Name as ClientName,im.candidateName,im.MobileNumber,im.IRFId,ulc.EmailId as ChannelOwner,IRF.JDTiltle,im.resumeLink
from InterviewMeetings im        
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId
Left join InterviewComments ic on ic.InterviewId=im.id      
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid   
left join UserList ul on ul.EmailId=im.interviewer
left join Interviewerprofile ip on ip.UserId=ul.id
left join UserList ulc on ulc.id=IRF.RisebirdHr
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc
outer apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imjf
outer apply(Select min(PerformedOn) FirstPerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imcf
where InterviewDate>@startDateParam and InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)  and IRF.ClientId=@Clientid
end
else if(@Role='SeniorHR' or @Role='SupportAdmin')
BEGIN  
Select Distinct im.id,IRF.id as irfIntId,im.interviewDate,imj.Joinedon,imjf.FirstJoinedon, ic.MeetingStatus,tzme.Joinedcount as TotalCount,
im.IsMeetingNotificationSent,im.IsNotificationSentToEI,im.IsRescheduleNotificationSent,im.IsEmergency,im.EIAssigned,imc.PerformedOn,imcf.FirstPerformedOn,
im.confirmationStatus,im.interviewer,im.candidateEmail,cl.Name as ClientName,im.candidateName,im.MobileNumber,im.IRFId,ulc.EmailId as ChannelOwner,IRF.JDTiltle,im.resumeLink
from InterviewMeetings im 
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId
join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid
Left join InterviewComments ic on ic.InterviewId=im.id      
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid
left join UserList ulc on ulc.id=IRF.RisebirdHr
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc
outer apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imjf
outer apply(Select min(PerformedOn) FirstPerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imcf
where ulsh.Emailid= @Emailid and InterviewDate>@startDateParam and InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)        
END
ELSE
BEGIN
Select Distinct im.id,IRF.id as irfIntId,im.interviewDate,imj.Joinedon,imjf.FirstJoinedon, ic.MeetingStatus,tzme.Joinedcount as TotalCount,
im.IsMeetingNotificationSent,im.IsNotificationSentToEI,im.IsRescheduleNotificationSent,im.IsEmergency,im.EIAssigned,imc.PerformedOn,imcf.FirstPerformedOn,
im.confirmationStatus,im.interviewer,im.candidateEmail,cl.Name as ClientName,im.candidateName,im.MobileNumber,im.IRFId
,ulc.EmailId as ChannelOwner,IRF.JDTiltle,im.resumeLink
from InterviewMeetings im
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId
Left join InterviewComments ic on ic.InterviewId=im.id      
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid  
left join UserList ulc on ulc.id=IRF.RisebirdHr
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc
outer apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imjf
outer apply(Select min(PerformedOn) FirstPerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imcf
where InterviewDate>@startDateParam and InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)  
END            
            
End
Go