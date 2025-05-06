
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewMeetings_ForInsert')
BEGIN
    Drop trigger tr_InterviewMeetings_ForInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create TRIGGER tr_InterviewMeetings_ForInsert
ON InterviewMeetings
FOR INSERT
AS
BEGIN
 Declare @id int, @candidateEmailId varchar(200)

 Select @id = Id from inserted

 select @candidateEmailId = Trim(candidateEmail) from InterviewMeetings where id = @id

 if(@candidateEmailId != (select candidateEmail from InterviewMeetings where id = @id))
 Begin
	 update InterviewMeetings set candidateEmail  = @candidateEmailId where id = @id
 End
 Declare @candiateEmailId varchar(200),
 @irfName varchar(15),
 @Confirmationstatus int

 Select @candiateEmailId=candidateEmail From InterviewMeetings where id=@id
 Select @irfName=IRFId From InterviewMeetings where id=@id
 Select @Confirmationstatus=confirmationStatus From InterviewMeetings where id=@id

 insert into InterviewMeetingsHistory 
 (id,	IRFId,	interviewDate,	interviewSlot,	interviewDuration,	candidateEmail,	ClientHrContact, resumeLink,
 confirmationStatus,	zoomLink,	risebirdHR,	interviewer,	IntervIewOnDevice,	PreferredLocation,
 ExpectedPerHourRate,	staffingAgency,	joiningTime,	sCreatedBy,	dtCreatedOn,	sModifiedBy,
 dtModifiedOn,	candidateName,	currentEmployerName	,MobileNumber,	QCInterviewer,	IsEmergency,
 InterviewBiometryScore,	JoiningBiometryScore,	HRCoordinatorEmailId,	HRComment	,HRImageFileURL,
 ZoomMeetingId,	IsMeetingNotificationSent,	NotificationSentDate,	IsNotificationSentToEI,
 IsRescheduleNotificationSent,	EIAssigned,	IsNotificationSentToCandidate,	isAutoCancelled,
 IsCandidateResponsed,	CandidateResponse,	ProctorReport,	Cancellationsource, ActionType, PerformedAt, PerformedBy, TimeZoneoffset,TimeZonesValue,IsDeleted,CancellationReason)

 (select id,	IRFId,	interviewDate,	interviewSlot,	interviewDuration,	@candidateEmailId,	ClientHrContact, resumeLink,
 confirmationStatus,	zoomLink,	risebirdHR,	interviewer,	IntervIewOnDevice,	PreferredLocation,
 ExpectedPerHourRate,	staffingAgency,	joiningTime,	sCreatedBy,	dtCreatedOn,	sModifiedBy,
 dtModifiedOn,	candidateName,	currentEmployerName	,MobileNumber,	QCInterviewer,	IsEmergency,
 InterviewBiometryScore,	JoiningBiometryScore,	HRCoordinatorEmailId,	HRComment	,HRImageFileURL,
 ZoomMeetingId,	IsMeetingNotificationSent,	NotificationSentDate,	IsNotificationSentToEI,
 IsRescheduleNotificationSent,	EIAssigned,	IsNotificationSentToCandidate,	isAutoCancelled,
 IsCandidateResponsed,	CandidateResponse,	ProctorReport,	Cancellationsource, 'Inserted', DATEADD(MINUTE, 330, getdate()), sCreatedBy, TimeZoneoffset, TimeZonesValue,IsDeleted,CancellationReason
 from InterviewMeetings where id = @id)
 
 if(@Confirmationstatus=1)
 begin
  Exec usp_AddCandidateStatusWebHookData @candiateEmailId, @irfName, 'scheduled' 
  end
END




