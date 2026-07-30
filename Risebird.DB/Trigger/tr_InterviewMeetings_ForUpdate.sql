
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewMeetings_ForUpdate')
BEGIN
    Drop trigger tr_InterviewMeetings_ForUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_InterviewMeetings_ForUpdate
ON InterviewMeetings
FOR UPDATE
AS
BEGIN

 Declare @id int, @candidateEmailId varchar(200), @initialInterviewer varchar(200), @currentInterviewer varchar(200)
 Declare @OldConfirmationstatus int,@Confirmationstatus int,@irfName varchar(20),@RedoInterview bit
 Select @id = Id from inserted

 select @candidateEmailId = Trim(candidateEmail),@Confirmationstatus=confirmationStatus,@irfName=IRFId,@RedoInterview=IsNull(RedoInterview,0) from InterviewMeetings where id = @id
 
 select @initialInterviewer = Trim(interviewer) from deleted
 select @currentInterviewer = Trim(interviewer) from inserted
 
 if(@initialInterviewer is not null and @initialInterviewer != @currentInterviewer)
 Begin

    Declare @initialInterviewerUserId int, @currentInterviewerUserId int, @irfId int

	select @initialInterviewerUserId = id from UserList where EmailId = @initialInterviewer
	select @currentInterviewerUserId = id from UserList where EmailId = @currentInterviewer
	select @irfId = id from IRF where IRFId = (select IRFId from InterviewMeetings where id = @id)


	declare @Enumerator table (id int)
	insert into @Enumerator    
	select id from TechSkillComments     
	where irfId = @irfId and UserId = @initialInterviewerUserId

	while exists (select 1 from @Enumerator)    
	begin    
		declare @idEnumerator int
		select top 1 @idEnumerator = id from @Enumerator

		if not exists(select id from TechSkillComments where userId = @currentInterviewerUserId 
		and Comment = (select Comment from TechSkillComments where id=@idEnumerator) 
		and SkillName = (select SkillName from TechSkillComments where id=@idEnumerator) and irfId = @irfId)
		Begin
			insert into TechSkillComments (Comment, Skill, IRFId, UserId, SkillName)
			(select Comment, Skill, @irfId, @currentInterviewerUserId, SkillName from TechSkillComments where id = @idEnumerator and TechSkillComments.SkillName is not null)
		End

		delete from @Enumerator where id = @idEnumerator  

	End

 End

set @OldConfirmationstatus=(Select top (1) confirmationStatus From InterviewMeetingsHistory where id=id order by PerformedAt desc)
 
 if(@candidateEmailId != (select candidateEmail from InterviewMeetings where id = @id))
 Begin
	 update InterviewMeetings set candidateEmail  = @candidateEmailId where id = @id
 End
 
 insert into InterviewMeetingsHistory 
 (id,	IRFId,	interviewDate,	interviewSlot,	interviewDuration,	candidateEmail,	ClientHrContact, resumeLink,
 confirmationStatus,	zoomLink,	risebirdHR,	interviewer,	IntervIewOnDevice,	PreferredLocation,
 ExpectedPerHourRate,	staffingAgency,	joiningTime,	sCreatedBy,	dtCreatedOn,	sModifiedBy,
 dtModifiedOn,	candidateName,	currentEmployerName	,MobileNumber,	QCInterviewer,	IsEmergency,
 InterviewBiometryScore,	JoiningBiometryScore,	HRCoordinatorEmailId,	HRComment	,HRImageFileURL,
 ZoomMeetingId,	IsMeetingNotificationSent,	NotificationSentDate,	IsNotificationSentToEI,
 IsRescheduleNotificationSent,	EIAssigned,	IsNotificationSentToCandidate,	isAutoCancelled,
 IsCandidateResponsed,	CandidateResponse,	ProctorReport,	Cancellationsource, ActionType, PerformedAt, PerformedBy, TimeZoneoffset, TimeZonesValue,IsDeleted,CancellationReason,BillingOwner)

 (select id, IRFId,	interviewDate, interviewSlot, interviewDuration, @candidateEmailId,	ClientHrContact, resumeLink,
 confirmationStatus, zoomLink, risebirdHR,	interviewer, IntervIewOnDevice,	PreferredLocation,
 ExpectedPerHourRate, staffingAgency,	joiningTime, sCreatedBy, dtCreatedOn, sModifiedBy,
 dtModifiedOn, candidateName, currentEmployerName, MobileNumber, QCInterviewer,	IsEmergency,
 InterviewBiometryScore, JoiningBiometryScore,	HRCoordinatorEmailId, HRComment, HRImageFileURL,
 ZoomMeetingId,	IsMeetingNotificationSent,	NotificationSentDate, IsNotificationSentToEI,
 IsRescheduleNotificationSent, EIAssigned, IsNotificationSentToCandidate, isAutoCancelled,
 IsCandidateResponsed,	CandidateResponse, ProctorReport, Cancellationsource, 'Updated', DATEADD(MINUTE, 330, getdate()), sModifiedBy, TimeZoneoffset, TimeZonesValue,IsDeleted,CancellationReason,BillingOwner
 from InterviewMeetings where id = @id) 
 
 if(@Confirmationstatus=2 and @Confirmationstatus!=@OldConfirmationstatus)
 begin
  Exec usp_AddCandidateStatusWebHookData @candidateEmailId, @irfName, 'cancelled' 
  Exec usp_AddAIInterviewStatusWebHookData @candidateEmailId, @irfName, 'cancelled' 
  end
 if(@RedoInterview=1)
 begin
 Exec usp_AddOrUpdateWalletDetails @id,'System','InterviewBilling','Dr',0
 end
END
GO