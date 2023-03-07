IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewMeetingsJoinTime')
BEGIN 

CREATE TABLE [dbo].[InterviewMeetingsJoinTime](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[interviewid] [int] NULL,
	[interviewer] [varchar](200) NULL,
	[Joinedon] [datetime] NULL,
 CONSTRAINT [PK_InterviewMeetingsJoinTime] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


