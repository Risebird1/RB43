  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerMeetingsbyId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerMeetingsbyId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetInterviewerMeetingsbyId]              
      @interviewId int      
AS              
Begin              
        
Declare @id int   
select ic.VideoLink  
from InterviewMeetings im join InterviewComments ic on ic.InterviewId =im.id                 
where im.id = @interviewId            
             
End   
GO

