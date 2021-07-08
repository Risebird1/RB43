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
@enddate datetime        
AS          
Begin         
    
Select Distinct im.id,im.interviewDate,ic.MeetingStatus,JoinedCount.TotalCount,im.IsMeetingNotificationSent,im.IsRescheduleNotificationSent from InterviewMeetings im    
Left join InterviewComments ic on ic.InterviewId=im.id    
Outer Apply(Select Count(Distinct ParticipantName) as TotalCount from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' and Meetingid=im.ZoomMeetingId )JoinedCount  
where InterviewDate>@startdate and InterviewDate<@enddate and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)    
        
        
End   
      
      
End 
GO
  