IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddorUpdateCallRecordingData'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddorUpdateCallRecordingData]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_AddorUpdateCallRecordingData
@interviewId int = null,
@IRFId varchar(50) = null,
@RecordingSid varchar(1000),
@CallSid varchar(50),
@callRecipient varchar(150),
@caller varchar(150),
@timeNow datetime,
@callRecipientPhoneNumber nvarchar(20)
AS
BEGIN

Begin try
	 begin tran

		declare @recipientProfileType varchar(20), @numericIRFId varchar(50)

		set @recipientProfileType = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile where EmailId = @callRecipient )


		if(@IRFId is not null)
		Begin
		if(ISNUMERIC(@IRFId) = 0)
		Begin
		set @numericIRFId = (select id from IRF where IRFId = @IRFId)
		End
		Else
		Begin
		set @numericIRFId = @IRFId
		End
		End


		if not Exists (Select 1 from CallRecordingData where [CallSid]= @CallSid)
		BEGIN

			Insert into CallRecordingData(InterviewId, IRFId, RecordingSid, [CallSid], CallRecipient, [Caller], RecipientProfileType, DateCreated, RecipientPhoneNumber)   
			values(@interviewId, @numericIRFId, @RecordingSid, @CallSid, @callRecipient,  @caller, @recipientProfileType, @timeNow, @callRecipientPhoneNumber) 


			if(@InterviewId is not null and @InterviewId > 0 and @recipientProfileType = 23)
			Begin

				if exists( select id from RisebirdCandidateCommunicationCount where InterviewId = @InterviewId)
					Begin
						Insert into RisebirdCandidateCommunicationCount (InterviewId, CallsCount, CreatedOn) values (@InterviewId, 1, DATEADD(MINUTE, 330, getdate()))
					End
					Else
					Begin
						Update RisebirdCandidateCommunicationCount set CallsCount = (CallsCount + 1), ModifiedOn = DATEADD(MINUTE, 330, getdate()) where InterviewId = @InterviewId
					End

			End


		END

		Else
		Begin
			Update CallRecordingData set RecordingSid=@RecordingSid where CallSid=@CallSid
		End



	 commit tran

End try
Begin catch
	rollback transaction
	DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),    
	@Severity int = ERROR_SEVERITY(),    
	@State smallint = ERROR_STATE()    
	RAISERROR (@Message, @Severity, @State)
End catch


END