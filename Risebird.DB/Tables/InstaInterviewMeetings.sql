IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InstaInterviewMeetings')
BEGIN

CREATE TABLE [dbo].[InstaInterviewMeetings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[RequestedDate] [datetime] NULL,
	[RequiredSkill] [varchar](150) NULL,
	[CandidateName] [varchar](100) NULL,
	[AssessmentGroup] [int] NULL,
	[ResumeLink] [varchar](1000) NULL,
	[JDLink] [varchar](1000) NULL,
	[RequestedBy] [varchar](150) NULL,
	[ConfirmationStatus] [int] NULL,
	[MeetingStatus] [int] NULL,
	[InterviewStatus] [int] NULL,
	[Interviewer] [varchar](150) NULL,
	[ZoomLink] [varchar](1000) NULL,
 CONSTRAINT [PK_InstaInterviewMeetings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


