/****** Object:  Table [dbo].[CandidateStatus]    Script Date: 28-01-2021 10.54.14 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerPersonalityScore')
BEGIN
CREATE TABLE [dbo].[InterviewerPersonalityScore](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[OverallPersonalityScore] [int] NULL,
	[Openness] [int] NULL,
	[Conscientiousness] [int] NULL,
	[Extroversion] [int] NULL,
	[Agreeableness] [int] NULL,
	[Neuroticism] [int] NULL,
	FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),

 CONSTRAINT [PK_InterviewerPersonalityScore] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
END
GO



