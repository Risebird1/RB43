
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
	[joiningTime][varchar](200) null,
	[IsApproved] [bit] NOT NULL,
	[ApprovalComments] [varchar](max) NOT NULL,
	[IsSelected] [bit] NOT NULL,
	[FinalComments] [varchar](max) NOT NULL,
	[sCreatedBy] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_sCreatedBy]  DEFAULT ('NA'),	
	[dtCreatedOn] [DateTime] NOT NULL CONSTRAINT [DF_InterviewMeetings_dtCreatedOn] DEFAULT ('NA'),	
	[sModifiedBy] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_sModifiedBy]  DEFAULT ('NA'),	
	[dtModifiedOn] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_dtModifiedOn]  DEFAULT ('NA'),	
 CONSTRAINT [PK_InterviewMeetings] PRIMARY KEY CLUSTERED 		
(		
	[id] ASC	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)		
)		

END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsApproved')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    ADD [IsApproved] BIT NOT NULL DEFAULT 1
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsSelected')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    ADD [IsSelected] BIT NOT NULL DEFAULT 1
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ApprovalComments')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    ADD [ApprovalComments] VARCHAR(max) NOT NULL DEFAULT 'Approved'
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FinalComments')
      AND Object_ID = Object_ID(N'InterviewMeetings'))
BEGIN
    ALTER TABLE InterviewMeetings
    ADD [FinalComments] VARCHAR(max) NOT NULL DEFAULT 'Approved'
END



END
GO

