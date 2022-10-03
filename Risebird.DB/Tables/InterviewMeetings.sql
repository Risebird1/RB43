
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewMeetings')
BEGIN  
CREATE TABLE [dbo].[InterviewMeetings](		
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [varchar](200) null,		
	[interviewDate] [DateTime] NULL,	
	[interviewSlot] [varchar](100) NOT NULL,
	[interviewDuration] [int] NULL,
	[candidateEmail] [varchar](200) NOT NULL,	
	[ClientHrContact] [varchar](200) NULL,	
	[resumeLink] [varchar](MAX)  NULL,
	[comfirmationStatus] [varchar](100) NOT NULL,	
	[zoomLink] [varchar](MAX) NOT NULL,
	[risebirdHR] [varchar](200) null,
	[interviewer] [varchar](200) null,	
	[IntervIewOnDevice] [varchar](500) null,
	[PreferredLocation][varchar] (200) null,
	[ExpectedPerHourRate][varchar](200) null,
	[staffingAgenecy][varchar](200) null,
	[QCInterviewer][varchar](200) null,
	[joiningTime][varchar](200) null,
	[IsEmergency] [bit] NOT NULL,
    HRCoordinatorEmailId VARCHAR(100),
    HRComment VARCHAR(max),
    HRImageFileURL VARCHAR(max),
	[InterviewBiometryScore] [decimal](10, 2) NULL,
	[JoiningBiometryScore] [decimal](10, 2) NULL,
	[sCreatedBy] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_sCreatedBy]  DEFAULT ('NA'),	
	[dtCreatedOn] [DateTime] NOT NULL CONSTRAINT [DF_InterviewMeetings_dtCreatedOn] DEFAULT ('NA'),	
	[sModifiedBy] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_sModifiedBy]  DEFAULT ('NA'),	
	[dtModifiedOn] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_dtModifiedOn]  DEFAULT ('NA'),	
    [ZoomMeetingId][varchar] (200),
    [IsMeetingNotificationSent][bit] NULL,
	[NotificationSentDate] [DateTime] NULL,	
    [IsNotificationSentToEI][bit] NULL,
    [IsRescheduleNotificationSent][bit] NULL,
    [EIAssigned][bit] NULL,
    [IsNotificationSentToCandidate][bit] NULL,
    [isAutoCancelled][bit] NULL,
    [IsCandidateResponsed][bit] NULL,
    [CandidateResponse][int] NULL,
    [ProctorReport][varchar](1000),
    [Cancellationsource][varchar](100),
    [HasDistinctParticipants][bit] NOT NULL Default 1,
    [RedoInterview][bit] NOT NULL Default 0,
    [BackUpLink] [varchar](MAX) NOT NULL,
    [BackUpLinkId][varchar] (200),
 CONSTRAINT [PK_InterviewMeetings] PRIMARY KEY CLUSTERED 		
(		
	[id] ASC	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)		
)		

END
ELSE
BEGIN
IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsApproved')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    DROP COLUMN  IsApproved
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsSelected')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    DROP COLUMN IsSelected
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ApprovalComments')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    DROP COLUMN ApprovalComments
END
IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FinalComments')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    DROP COLUMN FinalComments
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'QCInterviewer')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add QCInterviewer VARCHAR(200)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsEmergency')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add IsEmergency bit not null default 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewBiometryScore')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add InterviewBiometryScore DECIMAL (10,2)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JoiningBiometryScore')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add JoiningBiometryScore DECIMAL (10,2)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'HRCoordinatorEmailId')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add HRCoordinatorEmailId VARCHAR(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'HRComment')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add HRComment VARCHAR(max)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'HRImageFileURL')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add HRImageFileURL VARCHAR(max)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ZoomMeetingId')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN

    ALTER TABLE InterviewMeetings
    ADD ZoomMeetingId varchar(200)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsMeetingNotificationSent')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add IsMeetingNotificationSent bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'NotificationSentDate')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add NotificationSentDate datetime
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsNotificationSentToEI')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add IsNotificationSentToEI  bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsRescheduleNotificationSent')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add IsRescheduleNotificationSent  bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EIAssigned')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add EIAssigned  bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsNotificationSentToCandidate')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add IsNotificationSentToCandidate  bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'isAutoCancelled')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add isAutoCancelled  bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCandidateResponsed')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add IsCandidateResponsed  bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateResponse')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add CandidateResponse  int 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ProctorReport')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add ProctorReport  varchar(1000)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Cancellationsource')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add Cancellationsource varchar(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RoomId')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add RoomId varchar(35)
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ZoomMeetingId')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    alter column ZoomMeetingId varchar(200)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'HasDistinctParticipants')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add HasDistinctParticipants bit not null default 1
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RedoInterview')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add RedoInterview bit not null default 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BackUpLink')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add BackUpLink varchar(max)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BackUpLinkId')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    Add BackUpLinkId varchar(200)
END

END
GO

