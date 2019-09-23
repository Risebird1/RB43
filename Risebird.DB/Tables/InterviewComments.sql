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
GO




