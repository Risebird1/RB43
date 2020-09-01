/****** Object:  Table [dbo].[VideoSentiment]    Script Date: 28-08-2020 2.51.32 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'VideoSentiment')
BEGIN
CREATE TABLE [dbo].[VideoSentiment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[ImageUrl] [varchar](max) NULL,
	[EmotionType] [int] NOT NULL,
	[Value] [float] NOT NULL,
	FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),

 CONSTRAINT [PK_VideoSentiment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End
GO




