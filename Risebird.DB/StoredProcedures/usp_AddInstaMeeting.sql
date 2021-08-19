IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInstaMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInstaMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddInstaMeeting]  
@CandidateName varchar(100),  
@ResumeLink varchar(1000),  
@RequestId int
AS    
Begin    
    
 Insert into [InstaInterviewMeetings](RequestedDate,CandidateName,ResumeLink,RequestId)  
 values(dateadd(minute,330,GetDate()),@CandidateName,@ResumeLink,@RequestId)  
  
END 
Go