IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSentMailDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSentMailDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveSentMailDetails]    
@To varchar(max),     
@Cc varchar(max),     
@Bcc varchar(max),  
@MailBody varchar(max),  
@MailSubject varchar(500),
@sg_message_id varchar(50),
@IRFId int = null,
@InterviewId int = null,
@CreatedAt datetime,
@SentBy varchar(100)
AS    
Begin  

begin try
	begin tran

	Insert into SentMailDetails([To], Cc, Bcc, MailBody, MailSubject, sg_message_id, IRFId, InterviewId, CreatedAt, SentBy)  
	values(@To, @Cc, @Bcc, @MailBody, @MailSubject, @sg_message_id, @IRFId, @InterviewId, @CreatedAt, @SentBy)  



	if(@To is not null and @InterviewId is not null and @InterviewId > 0)
		Begin

		if exists (select top 1 ul.EmailId from UserList ul 
		cross apply 
		(
			select value from string_split(@To, ',')  where value = ul.EmailId
		) Rcp
		where ul.idProfile = 23)
		Begin
			if exists( select id from RisebirdCandidateCommunicationCount where InterviewId = @InterviewId)
			Begin
				Insert into RisebirdCandidateCommunicationCount (InterviewId, MailCount, CreatedOn) values (@InterviewId, 1, DATEADD(MINUTE, 330, getdate()))
			End
			Else
			Begin
				Update RisebirdCandidateCommunicationCount set MailCount = (MailCount + 1), ModifiedOn = DATEADD(MINUTE, 330, getdate()) where InterviewId = @InterviewId
			End
		
		End

		End

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

