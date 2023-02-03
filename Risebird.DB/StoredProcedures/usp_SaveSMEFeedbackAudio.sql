/****** Object:  StoredProcedure [dbo].[usp_SaveSMEFeedbackAudio]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSMEFeedbackAudio'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSMEFeedbackAudio
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



 
Create PROCEDURE [dbo].[usp_SaveSMEFeedbackAudio]     
@irfId int,
@interviewerProfileId int,
@feedbackAudioLink varchar(1000),
@timeNow datetime


AS      
BEGIN   
Begin Try
  Begin tran

	declare @interviewerEmail varchar(100) = (select ul.EmailId from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId where ipr.id =@interviewerProfileId)  
   if (@feedbackAudioLink is not null and @feedbackAudioLink != '')
   Begin
		update IRF set 
	    AudioComment = @feedbackAudioLink,
	    AudioCommentBy = @interviewerEmail,
	    UpdatedBySMEAt = @timeNow
	    where id = @irfId

		if not exists (select top 1 id from SMEActivityTracker where IRFId =@irfId and InterviewerProfileId = @interviewerProfileId)
		Begin
			insert into SMEActivityTracker (IRFId, InterviewerProfileId, IsAudioCommentRecorded)
			values (@irfId, @interviewerProfileId, 1)
		End
		Else
		Begin
			update SMEActivityTracker set IsAudioCommentRecorded = 1 where InterviewerProfileId = @interviewerProfileId and IRFId = @irfId
		End
   End

   
  
  commit tran
End try

Begin Catch

rollback transaction

DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
   @Severity int = ERROR_SEVERITY(),  
   @State smallint = ERROR_STATE()  
   
  RAISERROR (@Message, @Severity, @State)

End Catch


   
END   
GO


