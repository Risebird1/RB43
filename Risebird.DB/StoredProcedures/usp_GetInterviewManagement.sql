IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewManagement'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewManagement]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO       
  
         
    
           
Create PROCEDURE [dbo].[usp_GetInterviewManagement]    
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
if(@Role='SeniorHR')
BEGIN  
Select Distinct im.id,im.interviewDate,ic.MeetingStatus,tzme.Joinedcount as TotalCount,im.IsMeetingNotificationSent,im.IsRescheduleNotificationSent from InterviewMeetings im 
join IRF on IRF.IRFId=im.IRFId
join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid
Left join InterviewComments ic on ic.InterviewId=im.id      
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid    
where ulsh.Emailid= @Emailid and InterviewDate>@startDateParam and InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)        
END
ELSE
BEGIN
Select Distinct im.id,im.interviewDate,ic.MeetingStatus,tzme.Joinedcount as TotalCount,im.IsMeetingNotificationSent,im.IsRescheduleNotificationSent from InterviewMeetings im        
Left join InterviewComments ic on ic.InterviewId=im.id      
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid    
--Outer Apply(Select Count(Distinct ParticipantName) as TotalCount from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId )JoinedCount      
where InterviewDate>@startDateParam and InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)        
END            
            
End    
        
      
GO
  

