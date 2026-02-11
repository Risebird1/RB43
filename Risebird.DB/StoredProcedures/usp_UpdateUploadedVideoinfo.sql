IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUploadedVideoinfo'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUploadedVideoinfo
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_UpdateUploadedVideoinfo]           
@Id int,          
@videoLink varchar(1000) = null,          
@meetingtype varchar(50),
@transcriptSasUrl varchar(1000)=null
AS            
Begin           
if(@meetingtype='CalibrationMeetings')          
begin  
Update CalibrationMeetings set VideoLink=case when (@videoLink is not null  and @videoLink != '') then @videoLink else VideoLink end,
TranscriptLink=case when (@transcriptSasUrl is not null  and @transcriptSasUrl != '') then @transcriptSasUrl else TranscriptLink end where id=@Id  
end  
End  
GO