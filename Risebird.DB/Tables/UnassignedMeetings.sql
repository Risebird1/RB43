/****** Object:  Table [dbo].[UnassignedMeetings]    Script Date: 18-11-2019 6.45.47 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'UnassignedMeetings')
begin
CREATE TABLE [dbo].[UnassignedMeetings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[EmailId] [varchar](200) NOT NULL,
	FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_UnassignedMeetings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO



