/****** Object:  Table [dbo].[ProbableNoShowCandidates]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ProbableNoShowCandidates')
BEGIN
CREATE TABLE [dbo].[ProbableNoShowCandidates](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[CandidateUserId] [int] NOT NULL,
	[InterviewId][int] NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL
	CONSTRAINT FK_ProbableNoShowCandidates_IRFId FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	CONSTRAINT FK_ProbableNoShowCandidates_CandidateUserId FOREIGN KEY([CandidateUserId]) REFERENCES [dbo].[UserList] ([id]),
	CONSTRAINT FK_ProbableNoShowCandidates_InterviewId FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_ProbableNoShowCandidates] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

GO


