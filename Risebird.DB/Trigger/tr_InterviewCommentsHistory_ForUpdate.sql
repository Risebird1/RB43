
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
 Declare @id int, @interviewId int, @candiateEmailId varchar(200), @irfName varchar(25), @status varchar(50), @meetingStatus int
 Select @id = Id from inserted
 Select @meetingStatus = MeetingStatus from InterviewComments where id = @id

 Select @interviewId = InterviewId from InterviewComments where id = @id

 Declare @oldmeetingStatus int
 set @oldmeetingStatus=(Select top (1) MeetingStatus From InterviewCommentsHistory where InterviewId=@interviewId order by PerformedAt desc)

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

 if(@meetingStatus!=@oldmeetingStatus)
 begin
 Exec usp_AddOrUpdateWalletDetails @id,'System','InterviewBilling','Dr',0
 end
 if(@meetingStatus in (4, 6, 7, 8) and @oldmeetingStatus not in(4,6,7,8))
 Begin
	Select @candiateEmailId = candidateEmail from InterviewMeetings where id = @interviewId
	Select @irfName = IRFId from InterviewMeetings where id = @interviewId

	Exec usp_AddCandidateStatusWebHookData @candiateEmailId, @irfName, 'completed' 
 End

  if(@meetingStatus in (1, 5, 9) and @oldmeetingStatus not in (1, 5, 9))
 Begin
	Select @candiateEmailId = candidateEmail from InterviewMeetings where id = @interviewId
	Select @irfName = IRFId from InterviewMeetings where id = @interviewId

	Exec usp_AddCandidateStatusWebHookData @candiateEmailId, @irfName, 'noshow' 
 End
  
  if(@meetingStatus in (3) and @oldmeetingStatus not in (3))
 Begin
	Select @candiateEmailId = candidateEmail from InterviewMeetings where id = @interviewId
	Select @irfName = IRFId from InterviewMeetings where id = @interviewId

	Exec usp_AddCandidateStatusWebHookData @candiateEmailId, @irfName, 'dropped' 
 End
 

END
GO

