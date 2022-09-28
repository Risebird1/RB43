IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientAudioFeedbackForReport')

Begin
	CREATE TABLE [dbo].[ClientAudioFeedbackForReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[AudioUrl] [varchar](max) NULL,
	[CreatedAt][datetime] NULL,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](50) NULL
	CONSTRAINT [FK_ClientAudioFeedbackForReport_InterviewId] FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
	CONSTRAINT [PK_ClientAudioFeedbackForReport] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
End

Go

