
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateInstaInterviewReport'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateInstaInterviewReport
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
CREATE PROCEDURE [dbo].[usp_AddUpdateInstaInterviewReport]    
@InterviewId int,    
@IRFId int,    
@TechSkillComments varchar(500),    
@MeetingStatus int,    
@InterviewStatus int,    
@InterviewerComment varchar(500),    
@VideoLink varchar(1000)  
    
AS    
Begin    
    
IF EXISTS(Select 1 from InstaInterviewMeetings where id=@InterviewId)    
BEGIN    
 update InstaInterviewMeetings     
 set  TechSkillComments=ISNULL(@TechSkillComments,TechSkillComments),  
 MeetingStatus=@MeetingStatus,  
 InterviewStatus=@InterviewStatus,  
 VideoLink =ISNULL(@VideoLink,VideoLink),  
 InterviewerComment=ISNULL(@InterviewerComment,InterviewerComment)  
 Where id =@InterviewId    
    
 delete from TransInstaInterviewSkills where IRFId=@InterviewId    
END    
End    
  GO