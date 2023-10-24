IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewCommentsHistory')

BEGIN
CREATE TABLE [dbo].[InterviewCommentsHistory](
	[id] [int] NOT NULL,
	[InterviewId] [int] NOT NULL,
	[InterviewStatus] [int] NULL,
	[InterviewerComment] [varchar](max) NULL,
	[FraudCheckStatus] [int] NULL,
	[HRComment] [varchar](max) NULL,
	[QCStatus] [int] NULL,
	[QCComment] [varchar](max) NULL,
	[TrainingStatus] [int] NULL,
	[TrainerComment] [varchar](max) NULL,
	[AudioLink] [varchar](max) NULL,
	[VideoLink] [varchar](max) NULL,
	[L2InterviewStatus] [int] NULL,
	[ClientInterviewStatus] [int] NULL,
	[MeetingStatus] [int] NULL,
	[OfferStatus] [int] NULL,
	[PreparationStatus] [int] NULL,
	[JoiningStatus] [int] NULL,
	[IsApproved] [int] NULL,
	[IsSelected] [int] NULL,
	[ApprovalComments] [varchar](max) NULL,
	[FinalComments] [varchar](max) NULL,
	[JoiningComments] [varchar](max) NULL,
	[OfferComments] [varchar](max) NULL,
	[ThumbnailImage] [varchar](Max) NULL,
	[TotalView] [int] NULL,
	[IsShared] [varchar](10) NULL,
	[FeedbackAudioLink] [nvarchar](max) NULL,
	[AadharFileLink] [nvarchar](max) NULL,
	[MoveToCP] [bit] NULL,
    [CandidateAvailable] [bit] NULL,
    [IsLock] [bit] NULL,
    [ClientUserid] [int] NULL,
    [ThumbnailTimeSeconds][int] NULL,
    [VideoDuration][int] NULL,
    [InterviewerNickName][varchar](10) NULL,
    [CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedBy] [varchar](100) NULL,
	[PerformedAt][datetime] NULL,
	[PerformedBy][varchar](100) NULL,
	[ActionType][varchar](15) NULL,
	[ReportAccepted][bit])

End

Else
Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ActionType')
      AND Object_ID = Object_ID(N'InterviewCommentsHistory'))
BEGIN
    ALTER TABLE InterviewCommentsHistory
    Add ActionType varchar(15)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ReportAccepted')
      AND Object_ID = Object_ID(N'InterviewCommentsHistory'))
BEGIN
    ALTER TABLE InterviewCommentsHistory
    Add ReportAccepted bit
END

End
GO




