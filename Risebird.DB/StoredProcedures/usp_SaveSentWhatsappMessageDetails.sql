IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSentWhatsappMessageDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSentWhatsappMessageDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveSentWhatsappMessageDetails]    
@Recipient varchar(60),     
@CountryCode varchar(5) = null,     
@WhatsappNumber varchar(15) = null,  
@Message varchar(max),  
@ButtonLink varchar(500) = null,
@Event varchar(500) = null,
@IRFId int = null,
@InterviewId int = null,
@InterviewDate datetime = null,
@Header varchar(500) = null,
@CreatedAt datetime,
@SentBy varchar(100) =null
AS    
Begin  
begin try
	begin tran

		Insert into SentWhatsAppMessageDetails(Recipient, CountryCode, WhatsappNumber,	[Message], ButtonLink, [Event],	IRFId, InterviewId,	InterviewDate, Header, CreatedAt, SentBy)  
		values(@Recipient, @CountryCode, @WhatsappNumber, @Message, @ButtonLink, @Event, @IRFId, @InterviewId, @InterviewDate, @Header, @CreatedAt, @SentBy)  

		declare @recipientProfile varchar(50)
		set @recipientProfile = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile)
		if(@recipientProfile = 'Candidate' and @InterviewDate is not null)
		begin
			declare @interviewIdComputed int
			set @interviewIdComputed= (select top 1 id from InterviewMeetings where interviewDate = @InterviewDate and candidateEmail = @Recipient)
			if(@interviewIdComputed  is not null and @interviewIdComputed > 0)
			Begin
				if exists( select id from RisebirdCandidateCommunicationCount where InterviewId = @InterviewId)
					Begin
						Insert into RisebirdCandidateCommunicationCount (InterviewId, WhatsappCount, CreatedOn) values (@interviewIdComputed, 1, DATEADD(MINUTE, 330, getdate()))
					End
					Else
					Begin
						Update RisebirdCandidateCommunicationCount set WhatsappCount = (WhatsappCount + 1), ModifiedOn = DATEADD(MINUTE, 330, getdate()) where InterviewId = @interviewIdComputed
					End
			End
		end

	commit tran

end try

begin catch
	rollback transaction
	DECLARE @ErrorMessage varchar(MAX) = ERROR_MESSAGE(),    
	@Severity int = ERROR_SEVERITY(),    
	@State smallint = ERROR_STATE()    
	RAISERROR (@ErrorMessage, @Severity, @State)
end catch



End
GO

