/****** Object:  Table [dbo].[QuestionsDiscussed]    Script Date: 25-10-2019 7.55.04 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'QuestionsDiscussed')
Begin
CREATE TABLE [dbo].[QuestionsDiscussed](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[Comment] [varchar](max) NULL,
	[RatingId] [int] NOT NULL,
	[Timing] [int] NULL,
	[SkillName] [varchar](max) NULL,
	[SkillSet] [int] NULL,
	FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_QuestionsDiscussed] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End
GO



