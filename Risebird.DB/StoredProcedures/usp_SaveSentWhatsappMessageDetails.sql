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
@InterviewDate varchar(100) = null,
@Header varchar(500) = null,
@CreatedAt datetime,
@SentBy varchar(100) =null
AS    
Begin  
begin try
	begin tran
		declare @interviewIdComputed int = @InterviewId, @irfIntId int = @IRFId
		declare @recipientProfile varchar(50)
		set @recipientProfile = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile where ul.EmailId =@Recipient)
		if(@recipientProfile = 'Candidate' and @InterviewDate is not null)
		Begin

			declare @computedInterviewDate datetime

			select @computedInterviewDate = TRY_CONVERT(datetime, @InterviewDate)

			if(@computedInterviewDate is not null)
			begin
				set @interviewIdComputed= (select top 1 id from InterviewMeetings where interviewDate = @InterviewDate and candidateEmail = @Recipient)
					if(@interviewIdComputed is not null and @interviewIdComputed>0 and (@IRFId is null or @IRFId = 0))
					Begin
						set @irfIntId = (select id from IRF where IRFId = (select IRFId from InterviewMeetings where id = @interviewIdComputed))
					End
			End
		End
		

		Insert into SentWhatsAppMessageDetails(Recipient, CountryCode, WhatsappNumber,	[Message], ButtonLink, [Event],	IRFId, InterviewId,	InterviewDate, Header, CreatedAt, SentBy)  
		values(@Recipient, @CountryCode, @WhatsappNumber, @Message, @ButtonLink, @Event, @irfIntId, @interviewIdComputed, @computedInterviewDate, @Header, @CreatedAt, @SentBy)  

		
		if(@recipientProfile = 'Candidate' and @InterviewDate is not null)
		begin
			--declare @interviewIdComputed int, @irfIntId int = @IRFId
			--set @interviewIdComputed= (select top 1 id from InterviewMeetings where interviewDate = @InterviewDate and candidateEmail = @Recipient)
			--if(@interviewIdComputed is not null and @interviewIdComputed>0 and (@IRFId is null or @IRFId = 0))
			--Begin
			--	set @irfIntId = (select id from IRF where IRFId = (select IRFId from InterviewMeetings where id = @interviewIdComputed))
			--End
			--Else
			--Begin
			--	set @irfIntId = @IRFId
			--End
			if(@irfIntId  is not null and @irfIntId > 0)--@interviewIdComputed  is not null and @interviewIdComputed > 0
			Begin
				if not exists( select top 1 id from RisebirdCandidateCommunicationCount where IRFId = @irfIntId)
					Begin
						Insert into RisebirdCandidateCommunicationCount (IRFId, WhatsappCount, CreatedOn) values (@irfIntId, 1, DATEADD(MINUTE, 330, getdate()))
					End
				Else
					Begin
						Update RisebirdCandidateCommunicationCount set WhatsappCount = (WhatsappCount + 1), ModifiedOn = DATEADD(MINUTE, 330, getdate()) where IRFId = @irfIntId
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

