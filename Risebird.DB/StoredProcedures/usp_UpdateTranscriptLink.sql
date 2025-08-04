/****** Object:  StoredProcedure [dbo].[usp_UpdateTranscriptLink]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateTranscriptLink'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateTranscriptLink
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_UpdateTranscriptLink]         
@interviewId int, 
@transcriptLink varchar(2500) = null,
@clientExternalTranscriptUploadedUrl varchar(2500) = null
AS          
Begin         
       
update InterviewComments set

AudioTranscriptFileLink = case 
						when
						(@transcriptLink  is not null  and @transcriptLink  != '') then @transcriptLink  
						else AudioTranscriptFileLink 
						end,
ClientExternalTranscriptUploadedUrl = case 
						when
						(@clientExternalTranscriptUploadedUrl  is not null  and @clientExternalTranscriptUploadedUrl  != '') then @clientExternalTranscriptUploadedUrl  
						else ClientExternalTranscriptUploadedUrl 
						end
where InterviewId=@interviewId        
        

End        
GO



