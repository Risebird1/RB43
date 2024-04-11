IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSentMailDetailsBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSentMailDetailsBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveSentMailDetailsBulkInsert]    
(    
      @SentMailDetailsBulkData AS [dbo].[SentMailDetails_TableTypeV2] READONLY    
)
AS    
Begin  

begin try
	begin tran

	Insert into SentMailDetails([To], Cc, Bcc, MailBody, MailSubject, sg_message_id, IRFId, InterviewId, CreatedAt, SentBy, IsACS)

	select smd.[To], smd.Cc, smd.Bcc, smd.MailBody, smd.MailSubject, smd.sg_message_id, irf.id, smd.InterviewId, 
	smd.CreatedAt, smd.SentBy, smd.IsACS from @SentMailDetailsBulkData smd 
	join InterviewMeetings im on im.id = smd.InterviewId
	join IRF on irf.IRFId = im.IRFId

	commit tran
end try
begin catch
	rollback transaction    
	DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),    
	@Severity int = ERROR_SEVERITY(),    
	@State smallint = ERROR_STATE()    
	RAISERROR (@Message, @Severity, @State)
end catch



End
GO

