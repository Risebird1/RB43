/****** Object:  StoredProcedure [dbo].[usp_UpdateUploadedVideoLinkAndTranscriptLink]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUploadedVideoLinkAndTranscriptLink'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUploadedVideoLinkAndTranscriptLink
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_UpdateUploadedVideoLinkAndTranscriptLink]         
@interviewId int,        
@videoLink varchar(1000) = null,        
@videoDuration int = null,
@transcriptLink varchar(2500) = null
AS          
Begin         
        
update InterviewComments set VideoLink=@videoLink, VideoDuration = @videoDuration, AudioTranscriptFileLink =@transcriptLink where InterviewId=@interviewId        
        
declare @meetingStatus int
set @meetingStatus=(select MeetingStatus from InterviewComments where InterviewId=@interviewId)        
 
if(@meetingStatus is not null and @meetingStatus!=2)        
Begin        
 Exec usp_InsertOrUpdateInterviewPoints @interviewId   
End   

End        
GO


