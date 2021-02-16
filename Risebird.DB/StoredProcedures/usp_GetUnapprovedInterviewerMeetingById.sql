  
  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnapprovedInterviewerMeetingById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnapprovedInterviewerMeetingById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_GetUnapprovedInterviewerMeetingById]  
  
@interviewId int  
  
AS  
Begin  
  
SELECT im.id as imid  
     ,im.ISCId  
  ,interviewDate  
  ,interviewSlot  
  ,interviewDuration  
  ,candidateEmail  
  ,confirmationStatus  
  ,zoomLink  
  ,im.ZoomId 
  ,interviewer  
  ,candidateName  
  ,HRCoordinatorEmailId  
FROM UnapprovedInterviewerMeetings  im
inner join InterviewerSkillsChannel isc on im.ISCId = isc.InterviewerSkillId  
WHERE  
      [im].id = @interviewId  
End  
  
  