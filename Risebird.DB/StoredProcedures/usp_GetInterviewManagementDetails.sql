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
if(@Role='SeniorHR' or @Role='SupportAdmin')
BEGIN  
Select Distinct im.id,im.interviewDate,imj.Joinedon, ic.MeetingStatus,tzme.Joinedcount as TotalCount,im.IsMeetingNotificationSent,im.IsNotificationSentToEI,im.IsRescheduleNotificationSent,im.IsEmergency,im.EIAssigned,imc.PerformedOn,im.confirmationStatus,im.interviewer,im.candidateEmail from InterviewMeetings im 
join IRF on IRF.IRFId=im.IRFId
join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid
Left join InterviewComments ic on ic.InterviewId=im.id      
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid    
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id) imc
where ulsh.Emailid= @Emailid and InterviewDate>@startDateParam and InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)        
END
ELSE
BEGIN
Select Distinct im.id,im.interviewDate,imj.Joinedon, ic.MeetingStatus,tzme.Joinedcount as TotalCount,im.IsMeetingNotificationSent,im.IsNotificationSentToEI,im.IsRescheduleNotificationSent,im.IsEmergency,im.EIAssigned,imc.PerformedOn,im.confirmationStatus,im.interviewer,im.candidateEmail from InterviewMeetings im        
Left join InterviewComments ic on ic.InterviewId=im.id      
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid   
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id) imc
where InterviewDate>@startDateParam and InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)  
END            
            
End    
Go