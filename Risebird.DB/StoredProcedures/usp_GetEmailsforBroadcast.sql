/****** Object:  StoredProcedure [dbo].[usp_GetEmailsforBroadcast]    Script Date: 19-06-2020 12.59.29 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailsforBroadcast'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailsforBroadcast
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetEmailsforBroadcast]    
@Profile varchar(100),  
@timeNow datetime  
AS    
Begin   
  
  
IF (@Profile = 'Client')    
 BEGIN    
  select ClientHrContact from InterviewMeetings where interviewDate >= @timeNow and  ClientHrContact is not null and confirmationStatus=1
    
 End    
Else if(@Profile='Interviewer')  
 Begin  
  select interviewer from InterviewMeetings where interviewDate >= @timeNow  and interviewer is not null and confirmationStatus=1
    
 End  
  
Else  
 begin  
  select candidateEmail from InterviewMeetings where interviewDate >= @timeNow  and candidateEmail is not null and confirmationStatus=1
    
 End  
End  
  
GO


