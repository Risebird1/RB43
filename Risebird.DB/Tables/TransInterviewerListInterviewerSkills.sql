/****** Object:  Table [dbo].[TransInterviewerListInterviewerSkills]    Script Date: 5/15/2020 9:25:28 AM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerListInterviewerSkills')
BEGIN 

CREATE TABLE [dbo].[TransInterviewerListInterviewerSkills](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NULL,
	[ISCId] [int] NULL,
	[InterviewerProfileId] [int] NULL,
	[TotalInvites] [int] NOT NULL DEFAULT 0,
	FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
	FOREIGN KEY([ISCId]) REFERENCES [dbo].[InterviewerSkillsChannel] ([id]),
	FOREIGN KEY([InterviewerProfileId]) REFERENCES [dbo].[InterviewerProfile] ([id]),
 CONSTRAINT [PK_TransInterviewerListInterviewerSkills] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO




