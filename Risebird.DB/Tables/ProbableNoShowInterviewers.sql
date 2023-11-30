/****** Object:  Table [dbo].[ProbableNoShowInterviewers]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ProbableNoShowInterviewers')
BEGIN
CREATE TABLE [dbo].[ProbableNoShowInterviewers](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[InterviewerUserId] [int] NOT NULL,
	[InterviewId][int] NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL
	CONSTRAINT FK_ProbableNoShowInterviewers_IRFId FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	CONSTRAINT FK_ProbableNoShowInterviewers_CandidateUserId FOREIGN KEY([InterviewerUserId]) REFERENCES [dbo].[UserList] ([id]),
	CONSTRAINT FK_ProbableNoShowInterviewers_InterviewId FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_ProbableNoShowInterviewers] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

GO


