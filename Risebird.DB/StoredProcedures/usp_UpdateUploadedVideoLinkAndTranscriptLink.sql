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
@transcriptLink varchar(2500) = null,
@clientExternalVideoUploadedUrl varchar(2500) = null,
@clientExternalTranscriptUploadedUrl varchar(2500) = null,
@transcriptSummaryJson varchar(max) = null
AS          
Begin         


        
update InterviewComments 
set VideoLink=  case 
				when
				(@videoLink is not null  and @videoLink != '') then @videoLink 
				else VideoLink 
				end, 
VideoDuration = case 
				when
				(@videoDuration is not null) then @videoDuration 
				else VideoDuration 
				end,
AudioTranscriptFileLink = case 
						when
						(@transcriptLink  is not null  and @transcriptLink  != '') then @transcriptLink  
						else AudioTranscriptFileLink 
						end,
ClientExternalVideoUploadedUrl = case 
						when
						(@clientExternalVideoUploadedUrl  is not null  and @clientExternalVideoUploadedUrl  != '') then @clientExternalVideoUploadedUrl  
						else ClientExternalVideoUploadedUrl 
						end,
ClientExternalTranscriptUploadedUrl = case 
						when
						(@clientExternalTranscriptUploadedUrl  is not null  and @clientExternalTranscriptUploadedUrl  != '') then @clientExternalTranscriptUploadedUrl  
						else ClientExternalTranscriptUploadedUrl 
						end,
TranscriptSummary = @transcriptSummaryJson						
where InterviewId=@interviewId        
        
declare @meetingStatus int
set @meetingStatus=(select MeetingStatus from InterviewComments where InterviewId=@interviewId)        
 
if(@meetingStatus is not null and @meetingStatus!=2)        
Begin        
 Exec usp_InsertOrUpdateInterviewPoints @interviewId   
End   

End        
GO



