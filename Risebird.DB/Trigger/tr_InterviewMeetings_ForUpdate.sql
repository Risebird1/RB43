
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

 Select @id = Id from inserted

 select @candidateEmailId = Trim(candidateEmail) from InterviewMeetings where id = @id
 
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
	where irfId = @irfId and UserId = @initialInterviewer

	while exists (select 1 from @Enumerator)    
	begin    
		declare @idEnumerator int
		select top 1 @idEnumerator = id from @Enumerator

		if not exists(select id from TechSkillComments where userId = @currentInterviewerUserId 
		and Comment = (select Comment from TechSkillComments where id=@idEnumerator) 
		and Skill = (select Skill from TechSkillComments where id=@idEnumerator) and irfId = @irfId)
		Begin
			insert into TechSkillComments (Comment, Skill, IRFId, UserId)
			(select Comment, Skill, @irfId, @currentInterviewerUserId from TechSkillComments where id = @idEnumerator)
		End

		delete from @Enumerator where id = @idEnumerator  

	End

 End


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
 IsCandidateResponsed,	CandidateResponse,	ProctorReport,	Cancellationsource, ActionType, PerformedAt, PerformedBy, TimeZoneoffset, TimeZonesValue)

 (select id, IRFId,	interviewDate, interviewSlot, interviewDuration, @candidateEmailId,	ClientHrContact, resumeLink,
 confirmationStatus, zoomLink, risebirdHR,	interviewer, IntervIewOnDevice,	PreferredLocation,
 ExpectedPerHourRate, staffingAgency,	joiningTime, sCreatedBy, dtCreatedOn, sModifiedBy,
 dtModifiedOn, candidateName, currentEmployerName, MobileNumber, QCInterviewer,	IsEmergency,
 InterviewBiometryScore, JoiningBiometryScore,	HRCoordinatorEmailId, HRComment, HRImageFileURL,
 ZoomMeetingId,	IsMeetingNotificationSent,	NotificationSentDate, IsNotificationSentToEI,
 IsRescheduleNotificationSent, EIAssigned, IsNotificationSentToCandidate, isAutoCancelled,
 IsCandidateResponsed,	CandidateResponse, ProctorReport, Cancellationsource, 'Updated', DATEADD(MINUTE, 330, getdate()), sModifiedBy, TimeZoneoffset, TimeZonesValue
 from InterviewMeetings where id = @id) 
 

END
