IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewCommentsExtension')

BEGIN
CREATE TABLE [dbo].[InterviewCommentsExtension](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[InterviewStatus] [int] NULL,
	[MeetingStatus] [int] NULL,
	[InterviewerComment] [varchar](max) NULL,
	CONSTRAINT [FK_InterviewExtension] FOREIGN KEY (InterviewId) REFERENCES InterviewMeetings(id),
 CONSTRAINT [PK_InterviewCommentsExtension] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
End
GO


