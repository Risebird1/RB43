
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewCommentsHistory_ForUpdate')
BEGIN
    Drop trigger tr_InterviewCommentsHistory_ForUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_InterviewCommentsHistory_ForUpdate
ON InterviewComments
FOR UPDATE
AS
BEGIN
 Declare @id int
 Select @id = Id from inserted

 insert into InterviewCommentsHistory
 (id,	InterviewId,	InterviewStatus	,InterviewerComment	,FraudCheckStatus,	HRComment,	QCStatus,	QCComment,
 TrainingStatus,	TrainerComment,	AudioLink,	VideoLink,	L2InterviewStatus,	ClientInterviewStatus,	MeetingStatus,
 OfferStatus, PreparationStatus,	JoiningStatus,	IsApproved,	IsSelected,	ApprovalComments,	FinalComments,
 JoiningComments,	OfferComments, ThumbnailImage,	TotalView,	IsShared,	FeedbackAudioLink,	AadharFileLink,
 MoveToCP,	CandidateAvailable,	IsLock,	ClientUserid,	ThumbnailTimeSeconds,	VideoDuration,	InterviewerNickName,
 CreatedOn,	ModifiedOn,	CreatedBy,	ModifiedBy, PerformedAt, PerformedBy, ActionType)

 (select id,	InterviewId,	InterviewStatus	,InterviewerComment	,FraudCheckStatus,	HRComment,	QCStatus,	QCComment,
 TrainingStatus,	TrainerComment,	AudioLink,	VideoLink,	L2InterviewStatus,	ClientInterviewStatus,	MeetingStatus,
 OfferStatus, PreparationStatus,	JoiningStatus,	IsApproved,	IsSelected,	ApprovalComments,	FinalComments,
 JoiningComments,	OfferComments, ThumbnailImage,	TotalView,	IsShared,	FeedbackAudioLink,	AadharFileLink,
 MoveToCP,	CandidateAvailable,	IsLock,	ClientUserid,	ThumbnailTimeSeconds,	VideoDuration,	InterviewerNickName,
 CreatedOn,	ModifiedOn,	CreatedBy,	ModifiedBy, DATEADD(minute, 330, getdate()), ModifiedBy, 'Updated'
 from InterviewComments where id = @id )

END

