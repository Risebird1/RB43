IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AllEmergencyInterviewsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AllEmergencyInterviewsDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AllEmergencyInterviewsDetails]     
@startdate datetime,              
@enddate datetime
As
Begin
Select Distinct im.id,IRF.id as irfIntId,im.interviewDate,imj.Joinedon,imjf.FirstJoinedon, ic.MeetingStatus,tzme.Joinedcount as TotalCount,    
im.IsMeetingNotificationSent,im.IsNotificationSentToEI,im.IsRescheduleNotificationSent,im.IsEmergency,im.EIAssigned,imc.PerformedOn,imcf.FirstPerformedOn,    
im.confirmationStatus,im.interviewer,im.candidateEmail,cl.Name as ClientName,im.candidateName,im.MobileNumber,im.IRFId    
,ulc.EmailId as ChannelOwner,IRF.JDTiltle,im.resumeLink,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid
,iman.Interviewdate as LastInterviewDate,iman.MeetingStatus as LastMeetingStatus,im.ZoomMeetingId,im.ClientHrContact as ClientHR
from InterviewMeetings im            
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId    
Left join InterviewComments ic on ic.InterviewId=im.id          
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid      
left join UserList ulc on ulc.id=IRF.RisebirdHr    
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj    
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc    
outer apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imjf    
outer apply(Select min(PerformedOn) FirstPerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imcf    
outer Apply(Select top(1)ima.interviewDate,ica.MeetingStatus From InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where im.IRFId=ima.IRFId and im.candidateEmail=ima.candidateEmail and ima.confirmationStatus=1 order by ima.Interviewdate desc) iman
where im.InterviewDate>@startdate and im.InterviewDate<@enddate and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission) and im.EIAssigned=1
End
Go