/****** Object:  Table [dbo].[InterviewUnassigned]    Script Date: 04-03-2020 11.39.01 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewUnassigned')
BEGIN 

CREATE TABLE [dbo].[InterviewUnassigned](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MeetingId] [int] NOT NULL,
	[InterviewerEmailId] [nvarchar](200) NOT NULL,
	[InterviewDate] [datetime] NOT NULL,
	[UnassignedAt] [datetime] NOT NULL,
	FOREIGN KEY([MeetingId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_InterviewUnassigned] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO



