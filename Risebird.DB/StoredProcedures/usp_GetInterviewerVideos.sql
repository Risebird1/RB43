IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerVideos'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerVideos
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create procedure usp_GetInterviewerVideos   
  @Emailid varchar(100)  
  AS  
  BEGIN  
  Select top(5)ic.VideoLink,im.id as imid,(ul.FirstName+' '+ul.LastName) as FirstName
  from [dbo].[InterviewMeetings] im join InterviewerComments ic on im.id=ic.InterviewId 
  left join Userlist ul on im.candidateEmail=ul.EmailId
  where Interviewer=@Emailid and ic.VideoLink is not null  
  END