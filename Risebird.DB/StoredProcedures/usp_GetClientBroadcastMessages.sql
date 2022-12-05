/****** Object:  StoredProcedure [dbo].[usp_GetClientAudioFeedback]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientBroadcastMessages'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientBroadcastMessages
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_GetClientBroadcastMessages]  
AS
Begin   
  
select im.IRFId AS SkillChannelName, im.interviewer as Intervieweremail, im.candidateEmail, Subject, Message, SentAt, SentBy, im.interviewDate from ClientBroadcastMessages CAF 
left join InterviewMeetings im  on 
im.Id  = CAF.InterviewId 
 
End  
  
GO


