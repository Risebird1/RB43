/****** Object:  Table [dbo].[TransInterviewerListInterviewerSkills]    Script Date: 5/15/2020 9:25:28 AM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerListInterviewerSkills')
BEGIN 

CREATE TABLE [dbo].[TransInterviewerListInterviewerSkills](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[PrimarySkill] [int] NOT NULL,
	[InterviewerSkillId] [int] NOT NULL,
	[IsAdded] [bit] NULL,
 CONSTRAINT [PK_TransInterviewerListInterviewerSkills] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO




