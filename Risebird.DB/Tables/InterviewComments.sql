/****** Object:  Table [dbo].[InterviewComments]    Script Date: 23-09-2019 6.47.43 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewComments')

BEGIN
CREATE TABLE [dbo].[InterviewComments](
	[id] [int] IDENTITY(1,1) NOT NULL,
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
    [TotalTechScore][float] NULL,
	CONSTRAINT [FK_Interview] FOREIGN KEY (InterviewId) REFERENCES InterviewMeetings(id),

 CONSTRAINT [PK_InterviewComments] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UniqueConstraint] UNIQUE NONCLUSTERED 
(
	[InterviewId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End
Else 
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AadharFileLink')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add AadharFileLink nvarchar(MAX)
END


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FeedbackAudioLink')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add FeedbackAudioLink nvarchar(MAX)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ThumbnailImage')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add ThumbnailImage varchar(MAX)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalView')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add TotalView int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsShared')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add IsShared varchar(10)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MoveToCP')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add MoveToCP bit null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateAvailable')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add CandidateAvailable bit Not NUll default 1
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsLock')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add IsLock bit null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientUserid')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add ClientUserid int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ThumbnailTimeSeconds')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    ADD ThumbnailTimeSeconds int
END 

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VideoDuration')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add VideoDuration int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewerNickName')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add InterviewerNickName varchar(10)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedOn')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add CreatedOn datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedOn')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add ModifiedOn datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedBy')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add CreatedBy varchar(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedBy')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add ModifiedBy varchar(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalTechScore')
      AND Object_ID = Object_ID(N'InterviewComments'))
BEGIN
    ALTER TABLE InterviewComments
    Add TotalTechScore float
END

End
GO




