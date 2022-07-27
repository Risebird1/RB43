
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

 Declare @id int
 Select @id = Id from inserted
 
 insert into InterviewMeetingsHistory 
 (id,	IRFId,	interviewDate,	interviewSlot,	interviewDuration,	candidateEmail,	ClientHrContact, resumeLink,
 confirmationStatus,	zoomLink,	risebirdHR,	interviewer,	IntervIewOnDevice,	PreferredLocation,
 ExpectedPerHourRate,	staffingAgency,	joiningTime,	sCreatedBy,	dtCreatedOn,	sModifiedBy,
 dtModifiedOn,	candidateName,	currentEmployerName	,MobileNumber,	QCInterviewer,	IsEmergency,
 InterviewBiometryScore,	JoiningBiometryScore,	HRCoordinatorEmailId,	HRComment	,HRImageFileURL,
 ZoomMeetingId,	IsMeetingNotificationSent,	NotificationSentDate,	IsNotificationSentToEI,
 IsRescheduleNotificationSent,	EIAssigned,	IsNotificationSentToCandidate,	isAutoCancelled,
 IsCandidateResponsed,	CandidateResponse,	ProctorReport,	Cancellationsource, ActionType, PerformedAt, PerformedBy)

 (select id, IRFId,	interviewDate, interviewSlot, interviewDuration,	candidateEmail,	ClientHrContact, resumeLink,
 confirmationStatus, zoomLink, risebirdHR,	interviewer, IntervIewOnDevice,	PreferredLocation,
 ExpectedPerHourRate, staffingAgency,	joiningTime, sCreatedBy, dtCreatedOn, sModifiedBy,
 dtModifiedOn, candidateName, currentEmployerName, MobileNumber, QCInterviewer,	IsEmergency,
 InterviewBiometryScore, JoiningBiometryScore,	HRCoordinatorEmailId, HRComment, HRImageFileURL,
 ZoomMeetingId,	IsMeetingNotificationSent,	NotificationSentDate, IsNotificationSentToEI,
 IsRescheduleNotificationSent, EIAssigned, IsNotificationSentToCandidate, isAutoCancelled,
 IsCandidateResponsed,	CandidateResponse, ProctorReport, Cancellationsource, 'Updated', DATEADD(MINUTE, 330, getdate()), sModifiedBy
 from InterviewMeetings where id = @id) 
 

END
