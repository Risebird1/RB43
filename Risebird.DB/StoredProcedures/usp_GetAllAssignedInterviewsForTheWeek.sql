IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllAssignedInterviewsForTheWeek'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllAssignedInterviewsForTheWeek
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
    
Create PROCEDURE [dbo].[usp_GetAllAssignedInterviewsForTheWeek]     
   @emailID varchar(100),  
   @DateToday datetime  
AS    
BEGIN    
    select id, interviewDate from InterviewMeetings where cast(interviewDate as date)<=DATEADD(DAY, 7, cast(@DateToday as date)) and cast(interviewDate as date)>=DATEADD(DAY, -7, cast(@DateToday as date))   and interviewer=@emailID and confirmationStatus =1
END    
  
  