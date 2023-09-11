/****** Object:  Table [dbo].[TransInterviewersReliabilityScores]    Script Date: 04-12-2019 6.40.11 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewersReliabilityScores')
BEGIN 

CREATE TABLE [dbo].[TransInterviewersReliabilityScores](
			[Id] [int] IDENTITY(1,1) NOT NULL,		
			[IdInterviewer] [int] NOT NULL,		
			[IdInterviewMeeting] [int] NOT NULL,
            [CreatedinterviewDate] [DateTime] NULL,	
			CONSTRAINT [FK_InterviewRL_Interviewer] FOREIGN KEY (IdInterviewer) REFERENCES InterviewerProfile(id),
			CONSTRAINT [FK_InterviewRL_InterviewMeeting] FOREIGN KEY (IdInterviewMeeting) REFERENCES interviewmeetings(id),
 CONSTRAINT [PK_TransInterviewersReliabilityScores] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END

GO




