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
@SentBy varchar(100),
@IRFName varchar(20) = null
AS    
Begin  

begin try
	begin tran

	declare @irfIntId int

	if(@InterviewId is not null  and @InterviewId > 0)
	Begin
		set @irfIntId = (select id from IRF where IRFId = (select IRFId from InterviewMeetings where id = @InterviewId))
	End
	Else
	Begin
		if((@IRFId is null or @IRFId = 0) and @IRFName is not null)
		Begin
			set @irfIntId = (select id from IRF where IRFId = @IRFName)
		End
		Else
		Begin
			set @irfIntId = @IRFId
		End
	End

	

	Insert into SentMailDetails([To], Cc, Bcc, MailBody, MailSubject, sg_message_id, IRFId, InterviewId, CreatedAt, SentBy)  
	values(@To, @Cc, @Bcc, @MailBody, @MailSubject, @sg_message_id, @irfIntId, @InterviewId, @CreatedAt, @SentBy)  



	if(@To is not null and @irfIntId is not null and @irfIntId > 0) --@InterviewId is not null and @InterviewId > 0
		Begin

		if exists (select top 1 ul.EmailId from UserList ul 
		cross apply 
		(
			select value from string_split(@To, ',')  where value = ul.EmailId
		) Rcp
		where ul.idProfile = 23)
		Begin
			if not exists( select top 1 id from RisebirdCandidateCommunicationCount where IRFId = @irfIntId)
			Begin
				Insert into RisebirdCandidateCommunicationCount (IRFId, MailCount, CreatedOn) values (@irfIntId, 1, DATEADD(MINUTE, 330, getdate()))
			End
			Else
			Begin
				Update RisebirdCandidateCommunicationCount set MailCount = (MailCount + 1), ModifiedOn = DATEADD(MINUTE, 330, getdate()) where IRFId = @irfIntId
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

