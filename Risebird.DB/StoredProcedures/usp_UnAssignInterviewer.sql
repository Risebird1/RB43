   IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UnAssignInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_UnAssignInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
CREATE PROCEDURE [dbo].[usp_UnAssignInterviewer] @id int    
AS    
Begin Try    
      
 Declare @meetingId int    
    
 Set @meetingId = @id    
     
 IF EXISTS (SELECT 1 FROM UnapprovedInterviewerMeetings where Id = @meetingId)     
  BEGIN    
      UPDATE UnapprovedInterviewerMeetings Set interviewer = null where id = @meetingId    
  END    
     
  End Try     
    
  Begin Catch    
 RAISERROR ('Error While updating Interviwer', -- Message text.    
   16, -- Severity.    
   1 -- State.    
   );    
  End Catch    