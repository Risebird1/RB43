/****** Object:  StoredProcedure [dbo].[usp_GetTranscriptUrlByMeeting]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetTranscriptUrlByMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTranscriptUrlByMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetTranscriptUrlByMeeting]    
@interviewId int 
AS    
Begin    
    
   select AudioTranscriptFileLink from InterviewComments where InterviewId =@interviewId
    
End



GO


