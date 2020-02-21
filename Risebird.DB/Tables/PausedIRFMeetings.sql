/****** Object:  Table [dbo].[PausedIRFMeetings]    Script Date: 21-02-2020 1.48.35 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PausedIRFMeetings')
BEGIN 

CREATE TABLE [dbo].[PausedIRFMeetings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[MeetingId] [int] NULL,
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([MeetingId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_PausedIRFMeetings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO




