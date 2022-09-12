/****** Object:  StoredProcedure [dbo].[usp_GetClientCommentsForReport]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('[usp_GetClientAudioForIRF]'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientAudioForIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetClientAudioForIRF]  
  
@interviewId int  
  
AS  
Begin  

declare @irfId varchar(100)
set @irfId = (select IRFId from InterviewMeetings where id =@interviewId)

select AudioComment as AudioUrl from IRF where IRFId = @irfId

End  
  
  
GO


