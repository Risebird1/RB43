/****** Object:  StoredProcedure [dbo].[usp_GetClientAudioFeedback]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientAudioFeedback'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientAudioFeedback
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientAudioFeedback]  
AS
Begin   
  
 select im.IRFId AS SkillChannelName, im.interviewer as Intervieweremail, im.candidateEmail, AudioURL, CreatedAt, CreatedBy from ClientAudioFeedbackForReport CAF 
left join
InterviewMeetings im  on 
im.Id  = CAF.InterviewId 
 
End  
  
GO


