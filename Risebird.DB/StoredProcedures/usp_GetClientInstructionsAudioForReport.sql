/****** Object:  StoredProcedure [dbo].[usp_GetClientCommentsForReport]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('[usp_GetClientInstructionsAudioForReport]'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientInstructionsAudioForReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetClientInstructionsAudioForReport]  
  
@interviewId int,
@emailId varchar(50)
  
AS  
Begin  

select AudioUrl from ClientAudioFeedbackForReport where InterviewId = @interviewId and CreatedBy = @emailId

End  
  
  
GO


