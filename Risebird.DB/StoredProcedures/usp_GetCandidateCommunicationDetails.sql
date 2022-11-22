/****** Object:  StoredProcedure [dbo].[usp_GetCandidateCommunicationDetails]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateCommunicationDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateCommunicationDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetCandidateCommunicationDetails]  
@interviewId int,
@CommunicationMedium varchar(20)
AS  
Begin
	if(@CommunicationMedium = 'VoiceCall')
	Begin
		select CallRecipient, [Caller], RecordingUri from CallRecordingData where InterviewId = @interviewId
	End
	else if(@CommunicationMedium = 'Whatsapp')
	Begin
		select Recipient as WhatsappRecipient, [Message] as WhatsappMessage, ButtonLink as WhatsappButtonLink, [Event] as WhatsappEvent, InterviewDate from SentWhatsAppMessageDetails where InterviewId = @interviewId
	End
	else if(@CommunicationMedium = 'Email')
	Begin
	declare @candidateEmail varchar(100) = (select candidateEmail from InterviewMeetings where id = @interviewId)

		;WITH CTE AS
		(SELECT id,
			ROW_NUMBER() OVER (PARTITION BY email, Split_Message_Id ORDER BY [timestamp] desc) as rn FROM SendgridEventData
			where Split_Message_Id in (select sg_message_id from SentMailDetails where InterviewId = @interviewId)
		)
		SELECT id into #latestActivityRowId FROM CTE WHERE rn=1

  
		select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,
		sge.email as CandidateEmail, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],
		irf.IRFId as SkillChannelName, im.interviewDate as interviewDateMail
		from SentMailDetails smd
		join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id
		join UserList ul on ul.EmailId = sge.email
		left join IRF on smd.IRFId = irf.id 
		left join InterviewMeetings im on im.id = smd.InterviewId
		where sge.id in (select id from #latestActivityRowId) 
		and sge.email = @candidateEmail
  
		order by smd.CreatedAt desc
		drop table #latestActivityRowId
	End

End
GO


